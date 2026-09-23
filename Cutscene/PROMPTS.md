# Cutscene de abertura — Castle Knight

Prompts para gerar a animação no Freepik. Tudo em inglês (os modelos respondem
muito melhor). Copie e cole.

---

## Como usar no Freepik

1. **Imagem primeiro, vídeo depois.** Gere cada quadro-chave no *AI Image Generator*
   (modelo **Flux** ou **Mystic**), aprove, e só então mande para *AI Video* com
   image-to-video. Text-to-video direto perde a consistência do personagem.
2. Suba `referencia_personagens.png` (nesta pasta) como **imagem de referência /
   style reference** para o herói e o gato saírem parecidos com o jogo.
3. **Proporção 16:9**, e nos vídeos use **5 s por plano**.
4. Repita o bloco de ESTILO em *todos* os prompts — é o que mantém a unidade visual.
5. Se o modelo "suavizar" demais os pixels, acrescente `strict pixel grid, no
   anti-aliasing, hard pixel edges` e baixe a força da criatividade.

---

## Bloco de ESTILO (cole no início de todo prompt)

```
16-bit SNES-era pixel art, side-scrolling platformer game cutscene, limited color
palette, hard pixel edges, no anti-aliasing, chunky readable pixels, dark navy
outlines, warm saturated colors, dramatic rim lighting, parallax forest background
with layered silhouettes, cinematic wide shot, 16:9
```

## Prompt NEGATIVO (cole no campo negative de todos)

```
3d render, realistic, photorealistic, smooth gradients, blurry, anti-aliased,
modern vector art, anime screenshot, watermark, text, logo, ui, hud, extra limbs,
deformed hands, low contrast, washed out
```

---

## Os personagens (descrição fiel ao jogo)

**HERÓI** — cole quando ele aparecer:
```
a young pixel-art knight boy, short light-brown bob hair with bangs, big bright
blue eyes, red scarf around his neck, flowing red cape, silver-grey chest armor
with shoulder plates, white-silver bracers, dark green shorts, brown leather boots,
a sword with a golden hilt strapped across his back, tan skin, small heroic build
```

**A MOÇA / A GATA** — cole quando ela aparecer:
```
a gentle young woman in a simple long dress, long dark hair, warm expression
```
```
a small black cat, glossy black fur, large glowing amber-yellow eyes, pointed ears,
long curved tail held high, tiny pink nose
```

**A BRUXA** — cole quando ela aparecer:
```
a sinister tall witch, tattered deep-purple robe and pointed hat, long crooked
nose, glowing green eyes, bony hands crackling with green-purple magic, wisps of
dark smoke around her
```

**CENÁRIO**:
```
lush pixel-art forest clearing at golden hour, layered dark-teal foliage hills in
the background, turquoise sea on the far horizon, fluffy white clouds, brown dirt
ground with bright green grass edge, twisted trees with thick trunks
```

**Paleta do jogo** (se o modelo aceitar cores): `#804D36` marrom · `#AB4343` vermelho
da capa · `#9BADB7` prata da armadura · `#CBDBFC` azul claro · `#2D3835` verde escuro
· `#3E8E4E` verde da grama

---

## Roteiro — 7 planos, ~35 s

### PLANO 1 — A floresta em paz (5 s)
```
[ESTILO] + [CENÁRIO]
Wide establishing shot of a peaceful pixel-art forest clearing at golden hour.
Sunbeams cut through the trees. Butterflies drift. A small stone cottage sits at
the left edge. Nothing moves but the leaves. Calm, warm, safe.
Camera: very slow push in.
```

### PLANO 2 — A moça (5 s)
```
[ESTILO]
[A MOÇA] walking through the forest clearing, gathering red berries into a woven
basket, humming, unaware. Warm golden light on her face. Medium shot.
Camera: slow pan following her.
```

### PLANO 3 — A bruxa surge (5 s)
```
[ESTILO]
[A BRUXA] materializing from a swirl of dark purple smoke behind the young woman,
who turns in shock, dropping her basket, berries scattering in mid-air. The warm
golden light turns cold and green. Low angle, the witch towering over her.
Camera: quick push in, slight shake.
```

### PLANO 4 — O feitiço (5 s) ★ plano-chave
```
[ESTILO]
[A BRUXA] thrusting both bony hands forward, unleashing a swirling beam of
green-and-purple magic that engulfs [A MOÇA]. Her silhouette dissolves inside a
bright vortex of magical sparks and smoke. Blinding flash at the center.
Camera: locked wide shot, screen shaking.
```

### PLANO 5 — A transformação (5 s) ★ plano-chave
```
[ESTILO]
The magical smoke clears revealing [O GATO] sitting exactly where the woman stood,
looking at its own small paws in confusion and despair. The empty basket lies beside
it, berries scattered on the dirt. The witch cackles and vanishes into black smoke
in the background.
Camera: slow push in on the cat's face, ending on its glowing amber eyes.
```

### PLANO 6 — O herói viu tudo (5 s)
```
[ESTILO]
[O HERÓI] hiding behind a thick twisted tree trunk in the foreground, peeking out
with wide shocked blue eyes, his red cape fluttering. Out-of-focus in the
background, the black cat sits alone in the clearing. He grips the hilt of the
sword on his back, jaw set with determination.
Camera: rack focus from the boy to the cat.
```

### PLANO 7 — O pacto (6 s) — final
```
[ESTILO]
[O HERÓI] kneeling down and extending an open hand toward [O GATO]. The cat looks
up at him, hesitates, then steps forward and rubs against his hand. He smiles and
stands up, the cat at his heels. Both turn and walk to the right, into the golden
sunset, silhouetted against the glowing forest.
Camera: slow pull back to a wide silhouette shot.
```

---

## Cartões de texto (no editor, entre os planos)

Fonte do jogo: `Assets/_Game/Fonts/PressStart2P-Regular.ttf` — use ela para casar
com o menu. Texto amarelo `#FFD93B` sobre preto.

| Depois do plano | Texto |
|---|---|
| 1 | `UM REINO TRANQUILO...` |
| 4 | `A MALDICAO DA BRUXA` |
| 5 | `ELA PERDEU SUA FORMA HUMANA` |
| 7 | `CASTLE KNIGHT` (título grande, entra com o logo) |

## Trilha

Use a música do menu, que já existe no projeto:
`Assets/_Game/Audio/Music/menu.ogg`

---

## Depois de pronta

Exporte em **MP4, 1920x1080, 30 fps** e me mande o arquivo — eu coloco ela para
tocar antes do jogo (cena de vídeo antes do MainMenu, pulável com qualquer tecla,
usando o VideoPlayer do Unity).
