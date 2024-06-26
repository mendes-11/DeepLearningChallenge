import cv2
import numpy as np
import tensorflow as tf
import tempfile
import os

def segment_letters(image_path):
    img = cv2.imread(image_path, cv2.IMREAD_GRAYSCALE)
    
    # Binarização da imagem
    _, img = cv2.threshold(img, 127, 255, cv2.THRESH_BINARY_INV)
    
    # Aplicar dilatação e erosão
    kernel = np.ones((20,20),np.uint8)
    img = cv2.dilate(img, kernel, iterations=1)
    img = cv2.erode(img, kernel, iterations=1)
    
    # Aplicar flood fill para segmentar as letras
    h, w = img.shape[:2]
    mask = np.zeros((h+2, w+2), np.uint8)
    rects = []
    margin = 1
    for y in range(h):
        for x in range(w):
            if img[y, x] == 255 and mask[y+1, x+1] == 0:
                retval = cv2.floodFill(img, mask, (x, y), 255)
                rect = retval[3]
                expanded_rect = (max(rect[0] - margin, 0),
                                 max(rect[1] - margin, 0),
                                 min(rect[2] + 2 * margin, w - rect[0]),
                                 min(rect[3] + 2 * margin, h - rect[1]))
                rects.append(expanded_rect)
    rects = sorted(rects, key=lambda x: x[0])

    # Desenhar retângulos na imagem original
    img_with_rects = cv2.cvtColor(img, cv2.COLOR_GRAY2BGR)  # Converter para imagem colorida
    for rect in rects:
        x, y, w, h = rect
        # Adicionar uma borda vermelha em volta do retângulo
        cv2.rectangle(img_with_rects, (x, y), (x + w, y + h), (0, 0, 255), 2)

    # Salvar imagem com retângulos em um arquivo temporário
    temp_dir = tempfile.mkdtemp()
    temp_image_path = os.path.join(temp_dir, 'image_with_rects.png')
    cv2.imwrite(temp_image_path, img_with_rects)

    # Exibir a imagem com o visualizador padrão do sistema operacional
    os.system(f'start {temp_image_path}')

    return rects

def predict_letter(image_path, model_path):
    try:
        model = tf.keras.models.load_model(model_path)
    except ValueError as e:
        print(f"Erro ao carregar modelo: {e}")
        return None

    image = tf.keras.utils.load_img(image_path, target_size=(28, 28), color_mode='rgb')
    image = tf.keras.utils.img_to_array(image)
    image = np.expand_dims(image, axis=0)
    result = model.predict(image)
    indiceMaximo = np.argmax(result)
    if indiceMaximo < 10:
        return str(indiceMaximo)
    elif indiceMaximo < 36:
        return chr(indiceMaximo - 10 + ord('A'))
    else:
        return chr(indiceMaximo - 36 + ord('a'))


def process_image_and_identify_phrases(image_path, model_path, word_threshold=20):
    rects = segment_letters(image_path)
    if not rects:
        return []

    img = cv2.imread(image_path, cv2.IMREAD_GRAYSCALE)
    letters = []
    phrases = []

    sorted_rects = sorted(rects, key=lambda x: x[0])

    for i, rect in enumerate(sorted_rects):
        x, y, w, h = rect
        letter_image = img[y:y+h, x:x+w]
        letter_image_path = "temp.jpg"
        cv2.imwrite(letter_image_path, letter_image)
        letter = predict_letter(letter_image_path, model_path)
        letters.append(letter)

        if i < len(sorted_rects) - 1:
            next_x = sorted_rects[i+1][0]
            distance = next_x - (x + w)
            if distance > word_threshold:
                phrases.append(''.join(letters))
                letters = []

    phrases.append(''.join(letters))

    return phrases

phrases = process_image_and_identify_phrases('tests\\am.png', 'models\\m3.keras')
print("Frases identificadas:", phrases)
