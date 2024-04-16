import cv2
import numpy as np

def segment_letters(image_path):
    img = cv2.imread(image_path, cv2.IMREAD_GRAYSCALE)
    _, img = cv2.threshold(img, 127, 255, cv2.THRESH_BINARY_INV)
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
    img_color = cv2.cvtColor(img, cv2.COLOR_GRAY2BGR)
    for rect in rects:
        x, y, w, h = rect
        cv2.rectangle(img_color, (x, y), (x+w, y+h), (0, 255, 0), 1)
    cv2.imshow('Segmented Letters', img_color)
    cv2.waitKey(0)
    cv2.destroyAllWindows()
    return rects

segmented_letters = segment_letters('tests\\10.png')