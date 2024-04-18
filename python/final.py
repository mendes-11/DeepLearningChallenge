from flask import Flask, request, jsonify, send_file
import cv2
import numpy as np
import tensorflow as tf
import os
from werkzeug.utils import secure_filename
import threading

app = Flask(__name__)

UPLOAD_FOLDER = 'uploads'
if not os.path.exists(UPLOAD_FOLDER):
    os.makedirs(UPLOAD_FOLDER)
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER

model = tf.keras.models.load_model('models/m3.keras')

def segment_letters(image):
    _, img = cv2.threshold(image, 127, 255, cv2.THRESH_BINARY_INV)
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
    return rects

def predict_letter(image):
    image = cv2.resize(image, (28, 28))
    image = np.expand_dims(image, axis=-1)
    image = np.repeat(image, 3, axis=-1)
    image = np.expand_dims(image, axis=0)
    result = model.predict(image)
    indiceMaximo = np.argmax(result)
    if indiceMaximo < 10:
        return str(indiceMaximo)
    elif indiceMaximo < 36:
        return chr(indiceMaximo - 10 + ord('A'))
    else:
        return chr(indiceMaximo - 36 + ord('a'))

def process_image_and_identify_phrases(image, word_threshold=100):
    rects = segment_letters(image)

    if not rects:
        return []

    letters = []
    phrases = []

    sorted_rects = sorted(rects, key=lambda x: x[0])

    for i, rect in enumerate(sorted_rects):
        x, y, w, h = rect
        letter = predict_letter(image[y:y+h, x:x+w])
        letters.append(letter)

        if i < len(sorted_rects) - 1:
            next_x = sorted_rects[i+1][0]
            distance = next_x - (x + w)
            if distance > word_threshold:
                phrases.append(''.join(letters))
                letters = []

    if letters:
        phrases.append(''.join(letters))
        
    return phrases

def process_image(file_path):
    image = cv2.imread(file_path, cv2.IMREAD_GRAYSCALE)
    return process_image_and_identify_phrases(image)

@app.route('/upload', methods=['POST'])
def upload_file():
    if 'file' not in request.files:
        return jsonify({"error": "No file part"}), 400

    file = request.files['file']
    if file.filename == '':
        return jsonify({"error": "No selected file"}), 400

    if file:
        filename = secure_filename(file.filename)
        file_path = os.path.join(app.config['UPLOAD_FOLDER'], filename)
        file.save(file_path)

        def async_process():
            phrases = process_image(file_path)
            os.remove(file_path)
            return phrases

        phrases = async_process()

        return jsonify({"phrases": phrases, "message": "File uploaded successfully. Processing..."})

@app.route('/image/<filename>')
def get_image(filename):
    return send_file(os.path.join(app.config['UPLOAD_FOLDER'], filename))

if __name__ == '__main__':
    app.run(debug=True)
