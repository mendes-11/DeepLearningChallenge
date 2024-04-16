import tensorflow as tf
import numpy as np

def predict_letter(image_path, model_path):
    model = tf.keras.models.load_model(model_path)
    image = tf.keras.utils.load_img(image_path)
    data = np.array([image])
    result = model.predict(data)
    indiceMaximo = np.argmax(result)
    if indiceMaximo < 10:
        return str(indiceMaximo)
    elif indiceMaximo < 36:
        return chr(indiceMaximo - 10 + ord('A'))
    else:
        return chr(indiceMaximo - 36 + ord('a'))