# Design — Sistema de Inspeção de Vidraria

**Data**: 2026-05-19
**Branch**: `feat/cenario`
**Status**: aprovado pelo usuário, aguardando revisão do spec escrito

## Objetivo

Permitir que o jogador aponte para uma vidraria do laboratório, aperte E para pegá-la, e veja em primeira pessoa o objeto flutuando na frente da câmera enquanto um painel lateral exibe o nome e uma explicação científica do uso daquele item. E ou ESC devolve o objeto à posição original.

## Escopo

Inclui:
- Sistema de detecção por raycast (mira central) com highlight de hover.
- Mecânica de pegar/devolver objeto, reparentado em um anchor na frente da câmera.
- Rotação do objeto pelo mouse durante a inspeção.
- Travamento do `PlayerController` durante a inspeção (sem pausar o mundo).
- UI lateral com nome e descrição via TextMeshPro.
- ScriptableObject `GlasswareInfo` por vidraria.
- 6 vidrarias inspecionáveis no primeiro corte: Béquer, Erlenmeyer, Tubo de ensaio, Proveta, Balão de Florence, Lamparina.

Fora do escopo:
- Salvar progresso de "já inspecionei".
- Áudio.
- Animação de transição (Lerp) do objeto até a mão — teletransporta direto.
- Localização (textos só em PT-BR, plain string).
- Encher/aquecer/misturar líquidos.

## Decisões de design

| Aspecto | Decisão | Por quê |
|---|---|---|
| Estilo de inspeção | Objeto na mão + painel lateral | Imersivo, não pausa o jogo, casa com FPS existente |
| Detecção | Mira central + outline ao mirar | Descobrível e preciso |
| Dados | ScriptableObject por vidraria | Editor-friendly, versionável, texto longo no Inspector |
| Mundo durante | Player travado, mundo continua | Foco no objeto sem perder ambiência |
| Highlight | Material extra com outline shader (Cull Front + cor sólida) | Mais simples e barato que Renderer Feature do URP |
| Saída | E ou ESC | E reaproveita a tecla de pickup; ESC é porta de fuga padrão |

## Arquitetura

Duas camadas:

- **Mundo** (Player + objetos da cena): detecta o que o jogador mira, move a vidraria pra frente da câmera durante a inspeção.
- **UI** (Canvas): recebe `GlasswareInfo` e mostra/esconde o painel. Não conhece raycast nem o objeto 3D.

Cola: `InspectionController` ouve eventos da camada de mundo e chama métodos da UI.

Fluxo:

```
[mouse aponta] → Raycaster → Inspectable (highlight)
       ↓ [E]
InspectionController.Enter(target)
       ├─ trava PlayerController
       ├─ move objeto pra frente da câmera
       └─ InspectionUI.Show(target.info)
       ↓ [E ou ESC]
InspectionController.Exit()
       ├─ devolve objeto à posição original
       ├─ destrava PlayerController
       └─ InspectionUI.Hide()
```

### Estrutura de pastas

```
Assets/Scripts/
├── PlayerController.cs            (já existe)
├── Interaction/
│   ├── InteractionRaycaster.cs
│   ├── Inspectable.cs
│   └── InspectionController.cs
├── Data/
│   └── GlasswareInfo.cs           (ScriptableObject)
└── UI/
    └── InspectionUI.cs

Assets/Data/Glassware/             (assets .asset, um por vidraria)
```

## Componentes

### `GlasswareInfo` (ScriptableObject)

Dados puros, sem lógica. Criado pelo menu *Create → Virtual Lab → Glassware Info*.

```csharp
[CreateAssetMenu(menuName = "Virtual Lab/Glassware Info")]
public class GlasswareInfo : ScriptableObject {
    public string displayName;
    [TextArea(4, 10)] public string description;
}
```

### `Inspectable` (MonoBehaviour)

Marca um GameObject como pegável. Vive no prefab da vidraria. Layer dedicada `Inspectable`.

```csharp
public class Inspectable : MonoBehaviour {
    public GlasswareInfo info;
    public Renderer targetRenderer;     // arrastar no Inspector; padrão = GetComponentInChildren
    public Material outlineMaterial;    // material compartilhado de outline
    public void SetHighlight(bool on);
}
```

- `SetHighlight(true)` adiciona `outlineMaterial` ao array `targetRenderer.materials`; `SetHighlight(false)` remove (volta ao array original guardado em `Awake`).
- O material de outline é **um único asset compartilhado** em `Assets/Materials/Outline.mat` — todos os `Inspectable` apontam para o mesmo.
- `Awake` valida `info != null` e `outlineMaterial != null` com `Debug.LogWarning` se faltar.

### `InteractionRaycaster` (MonoBehaviour, no Player)

```csharp
public class InteractionRaycaster : MonoBehaviour {
    public float maxDistance = 2.5f;
    public LayerMask inspectableLayer;
    public KeyCode interactKey = KeyCode.E;
    public Transform rayOrigin;          // a câmera

    public event Action<Inspectable> OnPressed;
}
```

- `Update`: `Physics.Raycast(rayOrigin.position, rayOrigin.forward, maxDistance, inspectableLayer)`.
- Mantém `currentHover`; quando muda, chama `SetHighlight(false)` no antigo e `true` no novo.
- `GetKeyDown(interactKey)` com `currentHover != null` → emite `OnPressed(currentHover)`.
- Pode ser desativado pelo `InspectionController` durante `Inspecting` (`enabled = false`).

### `InspectionController` (MonoBehaviour, no Player)

Orquestrador. Máquina de estados `{ Idle, Inspecting }`.

```csharp
public class InspectionController : MonoBehaviour {
    public Transform holdAnchor;
    public float rotationSpeed = 200f;
    public PlayerController playerController;
    public InspectionUI ui;
    public InteractionRaycaster raycaster;
}
```

**Em `Awake`**: assina `raycaster.OnPressed += Enter`. Valida referências com `Assert.IsNotNull`.

**`Enter(Inspectable target)`** (só roda se `state == Idle`):
1. Salva snapshot: `originalParent`, `originalLocalPos`, `originalLocalRot`.
2. `target.SetHighlight(false)`.
3. Se tem `Rigidbody`, salva `isKinematic` e força `true`. Desabilita colliders.
4. Reparenta em `holdAnchor`, zera `localPosition` e `localRotation`.
5. `playerController.enabled = false`.
6. `raycaster.enabled = false`.
7. `ui.Show(target.info)`.
8. `state = Inspecting`.

**Em `Inspecting` (`Update`)**:
- Lê `Input.GetAxis("Mouse X/Y")`, gira `currentTarget` em `Vector3.up` (mundial) e `rayOrigin.right` (câmera).
- `Input.GetKeyDown(KeyCode.E)` ou `KeyCode.Escape` → `Exit()`.

**`Exit()`**:
1. `ui.Hide()`.
2. Reparenta o alvo em `originalParent`, restaura pose.
3. Restaura `isKinematic` e colliders.
4. `playerController.enabled = true`.
5. `raycaster.enabled = true`.
6. `state = Idle`. Limpa `currentTarget`.

### `InspectionUI` (MonoBehaviour, no Canvas)

```csharp
public class InspectionUI : MonoBehaviour {
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public GameObject panel;

    public void Show(GlasswareInfo info);
    public void Hide();
}
```

- `Show`: preenche os textos, `panel.SetActive(true)`.
- `Hide`: `panel.SetActive(false)`.

### Dependências (quem conhece quem)

- `Raycaster` → `Inspectable`.
- `Controller` → `Raycaster`, `PlayerController`, `InspectionUI`, alvo atual.
- `UI` → `GlasswareInfo`.
- `Inspectable` → `GlasswareInfo`.

Tudo injetado pelo Inspector. Nenhum `FindObjectOfType`.

## Casos de borda

| Caso | Tratamento |
|---|---|
| `Inspectable.info == null` | Raycaster ignora; `Inspectable.Awake` loga warning. |
| Referência não atribuída no Inspector | `Assert.IsNotNull` no `Awake`, falha rápido. |
| Rigidbody com física ativa | `Enter` força `isKinematic = true`; `Exit` restaura. |
| Tecla E pressionada no mesmo frame em que UI aparece | Máquina de estados: `OnPressed` só trata se `state == Idle`. Em `Inspecting`, E é consumida pelo Controller. |
| Player anda e perde o objeto da mira | `Raycaster` limpa `currentHover` no próximo frame. |
| Outline em objeto que não é vidraria | Filtrado por `LayerMask inspectableLayer` no `Raycast`. |
| Múltiplos `Inspectable` empilhados | `Physics.Raycast` retorna o mais próximo. |

## Testes

**EditMode tests** (`Assets/Tests/EditMode/`):
- `GlasswareInfoTests`: `ScriptableObject.CreateInstance<GlasswareInfo>()` aceita strings em `displayName` e `description`.
- `InspectionUITests`: instancia o prefab da UI, chama `Show(info)` e verifica que os textos foram preenchidos e o painel ativado; `Hide()` desativa.

**Validação manual** (caminhos que só fazem sentido em runtime):
- Hover acende e apaga outline.
- E perto da vidraria entra em inspeção; painel aparece; player não se move.
- Mouse gira o objeto nos dois eixos.
- E ou ESC devolve a vidraria à posição original.
- Pegar uma segunda vidraria após devolver a primeira funciona.
- Mirar parede/mesa não dispara prompt nem outline.

Sem PlayMode tests automatizados — custo de simular Input + raycast não compensa neste escopo.

## Setup de cena (passos manuais no Editor Unity)

1. **Layer**: criar layer `Inspectable` em *Edit → Project Settings → Tags and Layers*.
2. **Player**: adicionar `InteractionRaycaster` e `InspectionController` no GameObject `Player`. Criar `HoldAnchor` (GameObject vazio) como filho da `Main Camera` em `(0, -0.15, 0.5)`. Atribuir no Inspector. `rayOrigin` do Raycaster aponta para a Main Camera.
3. **Canvas**: criar Canvas (Screen Space - Overlay) com painel lateral direito contendo 2 `TextMeshProUGUI` (nome no topo, descrição abaixo). Adicionar `InspectionUI` no Canvas; arrastar referências. `panel` começa desativado.
4. **Conectar Player → UI**: arrastar o GameObject com `InspectionUI` no campo `ui` do `InspectionController`.
5. **ScriptableObjects**: criar 6 assets em `Assets/Data/Glassware/` (menu *Create → Virtual Lab → Glassware Info*):
   - `Beaker.asset` — Béquer
   - `Erlenmeyer.asset` — Erlenmeyer
   - `TestTube.asset` — Tubo de ensaio
   - `GraduatedCylinder.asset` — Proveta
   - `FlorenceFlask.asset` — Balão de Florence
   - `SpiritLamp.asset` — Lamparina

   Preencher `displayName` (PT-BR) e `description` (3-5 linhas, uso científico).
6. **Material de outline**: criar `Assets/Materials/Outline.mat` com um shader simples (URP/Unlit ou Custom com Cull Front + cor sólida). Material único, compartilhado entre todas as vidrarias.
7. **Prefabs**: para cada uma das 6 vidrarias em `Assets/ThidParty/3D Laboratory Environment with Appratus/Prefabs/`:
   - Atribuir layer `Inspectable` na raiz.
   - Confirmar que tem `Collider`.
   - Adicionar componente `Inspectable`:
     - `info`: arrastar o `GlasswareInfo` correspondente.
     - `targetRenderer`: arrastar o MeshRenderer principal (se nulo, busca em filhos).
     - `outlineMaterial`: arrastar `Outline.mat`.
8. **Cena**: posicionar as 6 vidrarias em `MainScene` sobre as mesas. Salvar.

## Resumo

| Aspecto | Decisão |
|---|---|
| Pickup | Mira central, tecla E, raycast 2.5m, layer `Inspectable` |
| Visualização | Objeto reparentado em `HoldAnchor`, painel lateral com nome + descrição |
| Durante | Player travado, mundo continua, mouse gira objeto |
| Dados | `GlasswareInfo` (ScriptableObject), 1 asset por vidraria |
| Highlight | Material extra (outline shader Cull Front), liga/desliga por script |
| Saída | E ou ESC, vidraria volta à pose/parent originais |
| Escopo | 6 vidrarias |
| Scripts novos | `GlasswareInfo`, `Inspectable`, `InteractionRaycaster`, `InspectionController`, `InspectionUI` |
