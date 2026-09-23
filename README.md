# Castle Knight — jogo 2D de plataforma

Projeto Unity 6.3 (6000.3.21f1), 2D URP. Menu + cutscene + tutorial + 4 fases
(floresta, caverna, inverno, castelo).

## Rodar

1. Abrir a pasta no Unity Hub.
2. Abrir `Assets/_Game/Scenes/MainMenu` e apertar Play.

> No Editor, o Play roda **a cena que está aberta** — a ordem do Build Settings só
> vale no `.exe`. Para ver o jogo do começo, abra sempre o `MainMenu`.

Clicar em **JOGAR** toca a cutscene (`Assets/_Game/Video/cutscene.mp4`) e depois
carrega o Tutorial; `Espaço` pula. Pelo menu **FASES** você entra direto na fase,
sem cutscene.

Fluxo: `JOGAR → cutscene → Tutorial → (volta ao menu)` e `FASES → Fase 1 → 2 → 3 →
(volta ao menu)`. O tutorial termina no menu porque não está em `ordemDasFases`
(no `GameManager`) — quem não está nessa lista volta para o menu ao terminar.

Se algum texto aparecer invisível: `Window → TextMeshPro → Import TMP Essential Resources`.

## Controles

| Ação | Teclado | Controle (Xbox) |
|---|---|---|
| Andar | Setas ou `A` / `D` | analógico esquerdo / direcional |
| Pular | `Espaço` (de novo no ar = pulo duplo) | `A` |
| Espada | `L` (ou `J` / clique) | `B` |
| Falar com a loja | `E` | `Y` |
| Pausar | `Esc` | `Start` |

Menus são navegáveis pelo controle (direcional + `A` para confirmar). O mapeamento
está no Input Manager e é lido por `Scripts/Core/GameInput.cs` — mudar uma tecla
é mexer em um lugar só.

O **Tutorial** é guiado: um balão acima do personagem mostra a instrução da vez (andar, pegar
diamante, pular, pulo duplo, atacar…) e avança quando você faz a ação; depois vêm dicas por
posição (buraco, espinhos, fruta, checkpoint, trampolim, águia). No menu, **FASES** abre a
seleção com um cartão por fase; toda fase começa com um cartão de apresentação (`FASE 1 —
FLORESTA` e um sprite do bioma) que some em 2,5 s ou com qualquer tecla.

Pise em cima dos inimigos ou corte com a espada. Serra e fogo não morrem; o fantasma
só cai pela espada. A espada também rebate a bola de fogo do chefe.
Cada fase tem 4 checkpoints (o gatilho é um feixe alto: passar por cima pulando também salva);
morrer 3 vezes volta ao menu.

**Chefes** no fim das fases 1–4 (o tutorial termina no troféu direto): ao entrar na arena, uma
parede fecha atrás e a barra de vida do chefe aparece no topo. Gambá Rei (3 golpes, investe e
pula), Lagarto de Fogo (4 golpes, cospe bolas de fogo), Yeti Gigante (4 golpes, rápido e
saltador) e **A Bruxa** (6 golpes, chefe final — carrega magia antes de lançar). Quanto menos
vida, mais frequentes os ataques. Pisão e espada tiram 1 cada.

**O gatinho preto** (`Scripts/Level/Companion.cs`) anda colado no herói com um atraso de
0,05 s — ele persegue o alvo de alguns quadros atrás, então segue os movimentos sem ficar
sincronizado demais; a velocidade copia a do herói e o termo proporcional só corrige o desvio,
para ele não viver atrasado. Tem física própria: cai, enxerga buraco antes de pisar nele e
calcula a força do pulo pela altura a vencer; o teleporte só acontece fora da tela. **Ele anota
onde você bateu o pé para pular e repete o salto no mesmo ponto** (com a mesma altura; pulo
duplo vira um pulo só, mais alto), em vez de reagir ao obstáculo quando já está encostando
nele. Parado, ele tem
ócio próprio: mia aos 7 s, senta aos 12 s e dorme aos 26 s. Miado e passinhos são sintetizados
no `RetroSfx`.

**Diamantes** são a moeda: contam na HUD e a cada 50 dão uma tentativa extra.
**Frutas** curam um coração — são raras e só podem ser pegas quando você está machucado
(com a vida cheia, ficam no lugar).

**Loja do Raposo** — uma por fase, logo antes da arena do chefe (no tutorial, depois do 2º
checkpoint). Chegue perto do raposo, aperte `E` (ou `Enter` / `↑`); o jogo pausa. Compre com
clique ou `1` / `2` / `3`:

| Item | Preço | Efeito |
|---|---|---|
| Fruta | 15 💎 | recupera 1 coração (só se estiver ferido) |
| Vida extra | 40 💎 | +1 tentativa |
| Coração extra | 70 💎 | +1 na vida máxima, até 5 — vale até o fim da partida |

Preços e itens ficam em `Scripts/UI/ShopUI.cs` (`Itens`).

## O jogo é gerado por código

Tudo que está em `Scenes/`, `Prefabs/`, `Animations/` e `Tiles/` é **gerado** a partir dos
sprites e das definições em `Scripts/Editor/`. Não edite essas pastas na mão — mude a
definição e regere:

```
Menu Unity:  Jogo → Reconstruir tudo (cenas, prefabs, animacoes)
```

Ou pela linha de comando, **a partir da pasta do projeto** e com o Unity **fechado**
(também tira capturas de tela em `Screenshots/`):

```bash
"C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod GameSetupWizard.ReconstruirBatch -logFile build.log
```

### Onde mexer para mudar o jogo

| Quero mudar... | Arquivo |
|---|---|
| O desenho das fases (chão, plataformas, inimigos, itens) | `Scripts/Editor/LevelDesigns.cs` |
| Quais tiles formam o chão de cada bioma | `Scripts/Editor/TileKits.cs` |
| As camadas de fundo (parallax) | `Scripts/Editor/Cenarios.cs` |
| Colisor, velocidade, animação de um prefab | `Scripts/Editor/PrefabBuilder.cs` |
| HUD, menu, pausa | `Scripts/Editor/UIBuilder.cs` |
| Máquina de estados do jogador | `Scripts/Editor/AnimationBuilder.cs` → `CriarControllerJogador` |

**Coordenadas das fases:** 1 tile = 1 unidade. `Chao(x, largura, altura)` cria um bloco
sólido a partir do chão; a superfície fica em `y = altura` (normalmente 4). `Plataforma(x, y, largura)`
cria uma plataforma flutuante cuja linha de baixo é `y`. `NoChao("Prefab", x, yChao)` põe algo
com os pés em `yChao`; `NoAr(...)` posiciona pelo centro.

## Estrutura

```
Assets/_Game/
├── Art/
│   ├── Adventurer/      herói (rvros) — um PNG por quadro
│   ├── SunnyLand/       floresta: tileset, fundo, gambá, águia, cereja, gema, props
│   ├── Grotto/          caverna: tileset, fundo, caranguejo, esqueleto, gosma, morcego...
│   ├── Winter/          inverno: tileset, fundo, raposa, yeti, coruja
│   ├── PixelAdventure/  armadilhas (serra, fogo, espinhos, trampolim), checkpoint, troféu
│   └── UI/Hearts/       corações da HUD
├── Audio/Music/         menu, floresta, caverna, inverno (.ogg)
├── Fonts/               Press Start 2P (TTF + TMP font asset gerado)
├── Animations/  Prefabs/  Scenes/  Tiles/     ← gerados
└── Scripts/
    ├── Core/      GameManager, LevelManager, CameraFollow, ParallaxLayer, AudioManager, RetroSfx
    ├── Player/    PlayerController2D, PlayerHealth
    ├── Enemies/   EnemyPatrol (chão ou voador), EnemyDamage (dano + pisão)
    ├── Level/     Collectible, Checkpoint, LevelEnd, Hazard, KillZone, MovingPlatform, Trampoline
    ├── UI/        HUDController, MainMenuController, PauseMenu
    └── Editor/    geradores (não entram no jogo)
```

`Assets/Scenes/SampleScene.unity` **não deve ser apagada**: é a base das cenas geradas,
porque traz a `Global Light 2D`. Sem ela, todo sprite fica preto no URP 2D.

## Detalhes técnicos que valem saber

- **Pixels Per Unit 16** em toda a arte: 1 tile de 16 px = 1 unidade = 1 célula do Grid.
- **Importação automática** (`SpriteImportSettings`): qualquer PNG em `Art/` vira sprite
  pixel-art (Point, sem compressão) e arquivos com `(LxA)` no nome são fatiados em grade.
- **Efeitos sonoros** são sintetizados em código (`RetroSfx`): pulo, moeda, dano, pisão,
  checkpoint, vitória, miado e passinhos do gato. Não há arquivos de SFX no projeto.
- **Luz 2D**: o gerador inclui todas as Sorting Layers na `Global Light 2D`; sem isso
  o que está em `Midground`/`Player` aparece em silhueta.
- **Input** em modo *Both*; o código usa o `Input` clássico.
- Inimigos atravessam uns aos outros (`PhysicsSetup`), para não se empurrarem para fora.

## Créditos dos assets

| Asset | Autor | Licença |
|---|---|---|
| Animated Pixel Adventurer | rvros | livre p/ uso, sem redistribuir |
| SunnyLand, SunnyLand Winter Forest, Super Grotto Escape, GothicVania Church (+ músicas) | ansimuz | livre p/ uso pessoal e comercial |
| Witches Pack — Blue Witch (chefe final) | 9E0 | livre p/ uso comercial, sem revender |
| Pixel Adventure 1 | Pixel Frog | CC0 |
| Hearts and health bar | VampireGirl | CC0 |
| Black Cat Sprites — versão completa (gato companheiro) | carysaurus | comprada; crédito obrigatório, sem redistribuir |
| Música "platformer_level03" | Pascal Belisle | crédito requerido |
| Press Start 2P | CodeMan38 | OFL |

A tela de Créditos do menu lista tudo isso.
