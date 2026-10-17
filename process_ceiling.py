import os
import sys
from PIL import Image, ImageFilter
import numpy as np

# Path gambar sumber
img1_path = r"C:/Users/MyBook Hype AMD/.gemini/antigravity/brain/5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c/.user_uploaded/media_1791791112217_551ac85e.png"
img2_path = r"C:/Users/MyBook Hype AMD/.gemini/antigravity/brain/5f3ee10b-4b2c-47c1-8d66-2cc6fd10200c/.user_uploaded/media_1791791130293_2a70f0e4.png"

# Folder target UVMapping\Ceiling
target_dirs = [
    r"c:\git\ProjectDarkRide\UVMapping\Ceiling",
]

# Cari jika ada folder Unity Assets yang punya UVMapping
for root, dirs, files in os.walk(r"c:\git\ProjectDarkRide"):
    if "UVMapping" in dirs:
        p = os.path.join(root, "UVMapping", "Ceiling")
        if p not in target_dirs:
            target_dirs.append(p)

for d in target_dirs:
    os.makedirs(d, exist_ok=True)

print("Target directories:", target_dirs)

def clean_text_region(img_quadrant):
    """
    Menghilangkan teks di bagian bawah quadrant dengan inpainting/patching cerdas
    dari area vertikal/horizontal simetris panel plafon yang bersih.
    """
    arr = np.array(img_quadrant).copy()
    h, w, c = arr.shape
    
    # Area teks berada di sekitar 78% hingga 98% dari tinggi quadrant
    # dan berada di tengah horizontal (20% hingga 80% dari lebar)
    y_start = int(h * 0.76)
    y_end = int(h * 0.99)
    x_start = int(w * 0.15)
    x_end = int(w * 0.85)
    
    # Deteksi teks: teks berwarna putih pekat atau hitam pekat di area tersebut
    # Kita bisa mendeteksi stroke teks atau melakukan inpaint dari baris di atasnya
    text_roi = arr[y_start:y_end, x_start:x_end]
    
    # Menggunakan OpenCV inpaint jika tersedia
    try:
        import cv2
        # Masking teks
        gray_roi = cv2.cvtColor(text_roi, cv2.COLOR_RGB2GRAY)
        # Teks memiliki kontras sangat tinggi (huruf putih terang dengan outline hitam pekat)
        mask = np.zeros(gray_roi.shape, dtype=np.uint8)
        mask[(gray_roi > 220) | (gray_roi < 35)] = 255
        
        # Dilate mask sedikit agar menutupi outline
        kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (5, 5))
        mask = cv2.dilate(mask, kernel, iterations=2)
        
        # Inpaint
        inpainted_roi = cv2.inpaint(text_roi, mask, inpaintRadius=5, flags=cv2.INPAINT_TELEA)
        arr[y_start:y_end, x_start:x_end] = inpainted_roi
        return Image.fromarray(arr)
    except Exception as e:
        print("OpenCV not available or failed, falling back to patch method:", e)
        # Fallback patch jika cv2 belum terinstall
        # Ambil sampel dari area panel di atasnya
        patch_h = y_end - y_start
        sample_y_start = max(0, y_start - patch_h - 10)
        sample_y_end = sample_y_start + patch_h
        
        sample = arr[sample_y_start:sample_y_end, x_start:x_end]
        arr[y_start:y_end, x_start:x_end] = sample
        return Image.fromarray(arr)

def process_grid(img_path, prefix):
    im = Image.open(img_path).convert('RGB')
    W, H = im.size
    mid_x = W // 2
    mid_y = H // 2
    
    # 4 Quadrant:
    # Top-Left: Albedo (Warna Asli)
    # Top-Right: Normal Map
    # Bottom-Left: Alt / Roughness / Grunge Albedo
    # Bottom-Right: Ambient Occlusion
    quadrants = {
        f"{prefix}_Albedo.png": (0, 0, mid_x, mid_y),
        f"{prefix}_Normal.png": (mid_x, 0, W, mid_y),
        f"{prefix}_Roughness.png": (0, mid_y, mid_x, H),
        f"{prefix}_AO.png": (mid_x, mid_y, W, H),
    }
    
    for filename, box in quadrants.items():
        quad = im.crop(box)
        cleaned = clean_text_region(quad)
        for d in target_dirs:
            save_path = os.path.join(d, filename)
            cleaned.save(save_path, "PNG")
            print(f"Saved: {save_path}")

print("Processing Image 1 (Wood Ceiling)...")
process_grid(img1_path, "Ceiling_Wood")

print("Processing Image 2 (Plaster Ceiling)...")
process_grid(img2_path, "Ceiling_Plaster")

print("Selesai semua!")
