import os
import sys
import numpy as np
from PIL import Image

# Import cv2 or use fallback
try:
    import cv2
    HAS_CV2 = True
except ImportError:
    HAS_CV2 = False

def inpaint_text_smart(img_pil):
    """
    Hanya menargetkan piksel huruf (warna putih terang + outline hitam pekat)
    di baris teks bagian bawah, TANPA menimpa kotak/patch besar.
    """
    arr = np.array(img_pil).copy()
    h, w, c = arr.shape
    
    # Area teks hanya berada di baris 87% sampai 97% tinggi gambar
    # dan berada di tengah (x: 5% sampai 95%)
    y1, y2 = int(h * 0.86), int(h * 0.98)
    x1, x2 = int(w * 0.05), int(w * 0.95)
    
    roi = arr[y1:y2, x1:x2]
    rh, rw, _ = roi.shape
    
    # Deteksi piksel teks
    # Huruf: Putih pekat (R,G,B > 220)
    # Outline: Hitam pekat (R,G,B < 30)
    r = roi[:, :, 0]
    g = roi[:, :, 1]
    b = roi[:, :, 2]
    
    # Mask untuk huruf dan border
    white_mask = (r > 215) & (g > 215) & (b > 215)
    black_mask = (r < 35) & (g < 35) & (b < 35)
    
    # Teks adalah kombinasi putih dan border hitam di sekitarnya
    text_mask = white_mask | black_mask
    
    if HAS_CV2:
        # Gunakan cv2 inpaint Telea
        mask_uint8 = (text_mask.astype(np.uint8)) * 255
        # Dilate 1 pixel agar menutupi antialiasing
        kernel = np.ones((3, 3), np.uint8)
        mask_uint8 = cv2.dilate(mask_uint8, kernel, iterations=1)
        
        inpainted_roi = cv2.inpaint(roi, mask_uint8, inpaintRadius=3, flags=cv2.INPAINT_TELEA)
        arr[y1:y2, x1:x2] = inpainted_roi
    else:
        # Algoritma inpaint murni Python/NumPy jika cv2 tidak ada:
        # Interpolasi horizontal/vertikal dari piksel non-teks terdekat
        mask_2d = text_mask
        cleaned_roi = roi.copy()
        
        # Untuk setiap piksel yang terkena mask, ambil rata-rata piksel bersih di atas dan bawahnya
        for y in range(rh):
            for x in range(rw):
                if mask_2d[y, x]:
                    # Cari piksel bersih di atas
                    y_top = y - 1
                    while y_top >= 0 and mask_2d[y_top, x]:
                        y_top -= 1
                    # Cari piksel bersih di bawah
                    y_bot = y + 1
                    while y_bot < rh and mask_2d[y_bot, x]:
                        y_bot += 1
                    
                    vals = []
                    if y_top >= 0:
                        vals.append(roi[y_top, x])
                    if y_bot < rh:
                        vals.append(roi[y_bot, x])
                    
                    if vals:
                        cleaned_roi[y, x] = np.mean(vals, axis=0).astype(np.uint8)
                    else:
                        # Fallback cari ke kiri/kanan
                        x_left = max(0, x - 1)
                        cleaned_roi[y, x] = roi[y, x_left]
        
        arr[y1:y2, x1:x2] = cleaned_roi

    return Image.fromarray(arr)

def process_file(src_path, prefix, output_dir):
    os.makedirs(output_dir, exist_ok=True)
    img = Image.open(src_path).convert('RGB')
    W, H = img.size
    halfW, halfH = W // 2, H // 2
    
    quads = {
        f"{prefix}_Albedo.png": (0, 0, halfW, halfH),
        f"{prefix}_Normal.png": (halfW, 0, W, halfH),
        f"{prefix}_Roughness.png": (0, halfH, halfW, H),
        f"{prefix}_AO.png": (halfW, halfH, W, H),
    }
    
    for filename, box in quads.items():
        cropped = img.crop(box)
        cleaned = inpaint_text_smart(cropped)
        out_path = os.path.join(output_dir, filename)
        cleaned.save(out_path, format="PNG")
        print(f"Berhasil dibuat rapi: {out_path}")

src1 = r"C:/Users/MyBook Hype AMD/.gemini/antigravity/brain/5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c/.user_uploaded/media_1791791112217_551ac85e.png"
src2 = r"C:/Users/MyBook Hype AMD/.gemini/antigravity/brain/5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c/.user_uploaded/media_1791791130293_2a70f0e4.png"
dest = r"c:\git\ProjectDarkRide\UVMapping\Ceiling"

print("Memproses Plafon Kayu (Wood)...")
process_file(src1, "Ceiling_Wood", dest)

print("Memproses Plafon Plester (Plaster)...")
process_file(src2, "Ceiling_Plaster", dest)

print("Semua selesai secara rapi!")
