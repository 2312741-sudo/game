"""Sinh SM_HangingBulb_v2_Socket.obj và _Glass.obj: bóng đèn tim treo (đui đèn + thủy tinh hình giọt nước), 2 mô hình riêng.
Gốc ở giữa thân; đỉnh đui đèn y=+0.09, đáy bóng y=-0.08 (mét)."""
import math
SEG = 28
top = 0.09
# (r, y) từ trên xuống; phần đui
socket = [(0.0, top), (0.016, top), (0.022, top - 0.004), (0.022, top - 0.014), (0.019, top - 0.016), (0.022, top - 0.022),
          (0.019, top - 0.025), (0.022, top - 0.031), (0.020, top - 0.036)]
yn = top - 0.036
glass = [(0.0200, yn)]
# thân thủy tinh: cổ nhỏ -> phình tròn -> đáy
for i in range(1, 15):
    t = i / 14
    y = yn - t * 0.133
    r = 0.020 + 0.034 * math.sin(min(t * 1.25, 1) * math.pi / 2) ** 1.4
    if t > 0.7:
        k = (t - 0.7) / 0.3
        r = max(0.0, r * math.sqrt(max(0.0, 1 - k * k)))
    glass.append((r, y))
V, VT, VN, F = [], [], [], []
def lathe(profile, group):
    base = len(V)
    n = len(profile)
    for j, (r, y) in enumerate(profile):
        for s in range(SEG + 1):
            a = 2 * math.pi * s / SEG
            V.append((r * math.cos(a), y, r * math.sin(a)))
            VT.append((s / SEG, 1 - j / (n - 1)))
            # pháp tuyến gần đúng: hướng ra ngoài, nghiêng theo độ dốc profile
            j0, j1 = max(j - 1, 0), min(j + 1, n - 1)
            dr, dy = profile[j1][0] - profile[j0][0], profile[j1][1] - profile[j0][1]
            nx, ny = dy, -dr
            ln = math.hypot(nx, ny) or 1
            VN.append(((nx / ln) * math.cos(a), ny / ln, (nx / ln) * math.sin(a)))
    out = []
    for j in range(n - 1):
        for s in range(SEG):
            a = base + j * (SEG + 1) + s
            b, c, d = a + 1, a + SEG + 1, a + SEG + 2
            out += [(a, c, b), (b, c, d)]
    F.append((group, out))
lathe(socket, 'Socket')
lathe(glass, 'Glass')
def write(path, name, group):
    # mỗi phần một OBJ riêng (importer OBJ gộp submesh nếu không có .mtl), chỉ số đỉnh đánh lại từ 1
    verts = {}
    lines = ['# %s (sinh bằng ArtSource/Scripts/make_hanging_bulb.py)' % name, 'o ' + name]
    tris = next(t for g, t in F if g == group)
    used = sorted({i for tri in tris for i in tri})
    remap = {old: k + 1 for k, old in enumerate(used)}
    for i in used: lines.append('v %.5f %.5f %.5f' % V[i])
    for i in used: lines.append('vt %.5f %.5f' % VT[i])
    for i in used: lines.append('vn %.5f %.5f %.5f' % VN[i])
    for a, b, c in tris:
        r = [remap[a], remap[b], remap[c]]
        lines.append('f ' + ' '.join('%d/%d/%d' % (x, x, x) for x in r))
    open(path, 'w').write('\n'.join(lines) + '\n')
write('Assets/TramChanh/Art/Models/Props/SM_HangingBulb_v2_Socket.obj', 'SM_HangingBulb_v2_Socket', 'Socket')
write('Assets/TramChanh/Art/Models/Props/SM_HangingBulb_v2_Glass.obj', 'SM_HangingBulb_v2_Glass', 'Glass')
print('verts', len(V), 'tris', sum(len(t) for _, t in F))
