import os
import cv2
import numpy as np

def preprocess_image(image):
    resized_image = cv2.resize(image, (128, 128))
    gray_image = cv2.cvtColor(resized_image, cv2.COLOR_BGR2GRAY)
    
    _, binary_image = cv2.threshold(gray_image, 127, 255, cv2.THRESH_BINARY)
    
    equalized_image = cv2.equalizeHist(binary_image)
    
    blurred_image = cv2.GaussianBlur(equalized_image, (1, 1), 0)
    
    dilate_struct = np.ones((5, 5), dtype=np.uint8)
    dilated_image = cv2.dilate(blurred_image, dilate_struct)
    
    erode_struct = np.ones((3, 3), dtype=np.uint8)
    eroded_image = cv2.erode(dilated_image, erode_struct)
    
    return eroded_image

pasta_destino = "./Fotos1"
pasta_origem = "./Img"
pasta_origem_files = os.listdir(pasta_origem)

for file in pasta_origem_files:
    origem_file_path = os.path.join(pasta_origem, file)
    imagem = cv2.imread(origem_file_path)
    
    imagem_processada = preprocess_image(imagem)
    
    cod = file[4:6]
    pasta_destino_cod = os.path.join(pasta_destino, cod)

    if not os.path.exists(pasta_destino_cod):
        os.makedirs(pasta_destino_cod)
    
    destino_file_path = os.path.join(pasta_destino_cod, file)
    cv2.imwrite(destino_file_path, imagem_processada)
