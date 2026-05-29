# Inspeção de Vidraria — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar sistema para o jogador apontar para uma vidraria, apertar E para inspecioná-la flutuando na frente da câmera, e ver nome + explicação científica num painel lateral.

**Architecture:** Cinco scripts pequenos e desacoplados (`GlasswareInfo` SO, `Inspectable`, `InteractionRaycaster`, `InspectionController`, `InspectionUI`). `Raycaster` detecta hover/press; `Controller` orquestra entrada/saída do estado de inspeção e reparenta o alvo num `HoldAnchor` da câmera; `UI` apenas exibe texto. Dados em ScriptableObjects, 1 asset por vidraria.

**Tech Stack:** Unity 2022.3.62f3, C#, TextMeshPro, Unity Test Framework (EditMode), Physics raycast com LayerMask.

**Premissas:**
- Branch atual: `feat/cenario` (continuar nela).
- Spec de referência: `docs/superpowers/specs/2026-05-19-inspecao-vidraria-design.md`.
- O usuário pode optar por não commitar — neste caso, pular os passos `git commit` e seguir.

---

## Task 1: Criar assembly definitions para gameplay e testes

Sem asmdef o EditMode test não consegue referenciar os scripts. Vamos criar um asmdef para o código (`Assets/Scripts/`) e outro para os testes (`Assets/Tests/EditMode/`).

**Files:**
- Create: `Assets/Scripts/VirtualLab.asmdef`
- Create: `Assets/Tests/EditMode/VirtualLab.EditMode.asmdef`

- [ ] **Step 1.1: Criar `Assets/Scripts/VirtualLab.asmdef`**

```json
{
    "name": "VirtualLab",
    "rootNamespace": "",
    "references": [
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 1.2: Criar `Assets/Tests/EditMode/VirtualLab.EditMode.asmdef`**

```json
{
    "name": "VirtualLab.EditMode",
    "rootNamespace": "",
    "references": [
        "VirtualLab",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ]
}
```

- [ ] **Step 1.3: Verificar compilação no Unity Editor**

Abrir Unity. Em *Window → General → Console*. Não deve haver erros vermelhos. Se aparecer "PlayerController not found in assembly", confirmar que `Assets/Scripts/PlayerController.cs` foi recompilado dentro do novo assembly `VirtualLab` (ele será movido automaticamente porque está na mesma pasta da asmdef).

- [ ] **Step 1.4: Commit (pular se for não commitar)**

```bash
git add Assets/Scripts/VirtualLab.asmdef Assets/Tests/EditMode/VirtualLab.EditMode.asmdef
git commit -m "chore: add asmdefs for gameplay and editmode tests"
```

---

## Task 2: `GlasswareInfo` ScriptableObject + teste

**Files:**
- Create: `Assets/Scripts/Data/GlasswareInfo.cs`
- Create: `Assets/Tests/EditMode/GlasswareInfoTests.cs`

- [ ] **Step 2.1: Escrever o teste falho**

Criar `Assets/Tests/EditMode/GlasswareInfoTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class GlasswareInfoTests
{
    [Test]
    public void StoresDisplayNameAndDescription()
    {
        var info = ScriptableObject.CreateInstance<GlasswareInfo>();
        info.displayName = "Béquer";
        info.description = "Recipiente de vidro borossilicato para misturar e aquecer líquidos.";

        Assert.AreEqual("Béquer", info.displayName);
        Assert.AreEqual("Recipiente de vidro borossilicato para misturar e aquecer líquidos.", info.description);
    }
}
```

- [ ] **Step 2.2: Rodar o teste e confirmar que falha**

Abrir *Window → General → Test Runner*. Aba *EditMode*. Clicar *Run All*.

Esperado: erro de compilação `The type or namespace 'GlasswareInfo' could not be found` (porque a classe ainda não existe).

- [ ] **Step 2.3: Criar `Assets/Scripts/Data/GlasswareInfo.cs`**

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Virtual Lab/Glassware Info", fileName = "GlasswareInfo")]
public class GlasswareInfo : ScriptableObject
{
    public string displayName;

    [TextArea(4, 10)]
    public string description;
}
```

- [ ] **Step 2.4: Rodar o teste de novo e confirmar PASS**

Voltar no *Test Runner*, *Run All*. Esperado: `GlasswareInfoTests.StoresDisplayNameAndDescription` PASS (verde).

- [ ] **Step 2.5: Commit (pular se for não commitar)**

```bash
git add Assets/Scripts/Data Assets/Tests/EditMode/GlasswareInfoTests.cs
git commit -m "feat: add GlasswareInfo ScriptableObject"
```

---

## Task 3: `InspectionUI` MonoBehaviour + teste

**Files:**
- Create: `Assets/Scripts/UI/InspectionUI.cs`
- Create: `Assets/Tests/EditMode/InspectionUITests.cs`

- [ ] **Step 3.1: Escrever os testes falhos**

Criar `Assets/Tests/EditMode/InspectionUITests.cs`:

```csharp
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class InspectionUITests
{
    private static InspectionUI BuildUI(out GameObject panel, out TMP_Text name, out TMP_Text desc)
    {
        var root = new GameObject("UI");
        var ui = root.AddComponent<InspectionUI>();

        panel = new GameObject("Panel");
        panel.transform.SetParent(root.transform);
        panel.SetActive(false);

        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(panel.transform);
        name = nameGO.AddComponent<TextMeshPro>();

        var descGO = new GameObject("Desc");
        descGO.transform.SetParent(panel.transform);
        desc = descGO.AddComponent<TextMeshPro>();

        ui.panel = panel;
        ui.nameText = name;
        ui.descriptionText = desc;
        return ui;
    }

    [Test]
    public void Show_ActivatesPanelAndFillsText()
    {
        var ui = BuildUI(out var panel, out var name, out var desc);
        var info = ScriptableObject.CreateInstance<GlasswareInfo>();
        info.displayName = "Béquer";
        info.description = "Usado para medir e aquecer líquidos.";

        ui.Show(info);

        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Béquer", name.text);
        Assert.AreEqual("Usado para medir e aquecer líquidos.", desc.text);
    }

    [Test]
    public void Hide_DeactivatesPanel()
    {
        var ui = BuildUI(out var panel, out _, out _);
        panel.SetActive(true);

        ui.Hide();

        Assert.IsFalse(panel.activeSelf);
    }
}
```

- [ ] **Step 3.2: Rodar e confirmar falha**

*Test Runner → EditMode → Run All*. Esperado: erro de compilação `The type or namespace 'InspectionUI' could not be found`.

- [ ] **Step 3.3: Criar `Assets/Scripts/UI/InspectionUI.cs`**

```csharp
using TMPro;
using UnityEngine;

public class InspectionUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public GameObject panel;

    public void Show(GlasswareInfo info)
    {
        if (info == null) return;
        if (nameText != null) nameText.text = info.displayName;
        if (descriptionText != null) descriptionText.text = info.description;
        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}
```

- [ ] **Step 3.4: Rodar testes e confirmar PASS**

*Test Runner → Run All*. Esperado: ambos `Show_...` e `Hide_...` PASS.

- [ ] **Step 3.5: Commit (pular se for não commitar)**

```bash
git add Assets/Scripts/UI Assets/Tests/EditMode/InspectionUITests.cs
git commit -m "feat: add InspectionUI with show/hide for glassware text"
```

---

## Task 4: `Inspectable` MonoBehaviour

Sem teste automatizado: a interação com `Renderer.materials` é mais barata de validar manualmente. A validação fica no playtest.

**Files:**
- Create: `Assets/Scripts/Interaction/Inspectable.cs`

- [ ] **Step 4.1: Criar `Assets/Scripts/Interaction/Inspectable.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Inspectable : MonoBehaviour
{
    public GlasswareInfo info;
    public Renderer targetRenderer;
    public Material outlineMaterial;

    private Material[] originalMaterials;
    private bool isHighlighted;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (info == null)
        {
            Debug.LogWarning($"Inspectable em {name} sem GlasswareInfo atribuído.", this);
        }
        if (outlineMaterial == null)
        {
            Debug.LogWarning($"Inspectable em {name} sem outlineMaterial atribuído.", this);
        }
        if (targetRenderer != null)
        {
            originalMaterials = targetRenderer.sharedMaterials;
        }
    }

    public void SetHighlight(bool on)
    {
        if (targetRenderer == null || outlineMaterial == null) return;
        if (on == isHighlighted) return;

        if (on)
        {
            var withOutline = new List<Material>(targetRenderer.sharedMaterials) { outlineMaterial };
            targetRenderer.materials = withOutline.ToArray();
        }
        else
        {
            targetRenderer.materials = originalMaterials;
        }
        isHighlighted = on;
    }
}
```

- [ ] **Step 4.2: Confirmar compilação no Editor**

Voltar ao Unity, conferir Console — sem erros vermelhos.

- [ ] **Step 4.3: Commit (pular se for não commitar)**

```bash
git add Assets/Scripts/Interaction/Inspectable.cs
git commit -m "feat: add Inspectable component with outline highlight"
```

---

## Task 5: `InteractionRaycaster` MonoBehaviour

**Files:**
- Create: `Assets/Scripts/Interaction/InteractionRaycaster.cs`

- [ ] **Step 5.1: Criar `Assets/Scripts/Interaction/InteractionRaycaster.cs`**

```csharp
using System;
using UnityEngine;

public class InteractionRaycaster : MonoBehaviour
{
    public Transform rayOrigin;
    public float maxDistance = 2.5f;
    public LayerMask inspectableLayer;
    public KeyCode interactKey = KeyCode.E;

    public event Action<Inspectable> OnPressed;

    private Inspectable currentHover;

    private void Awake()
    {
        if (rayOrigin == null)
        {
            Debug.LogError($"InteractionRaycaster em {name} sem rayOrigin (Camera).", this);
            enabled = false;
        }
    }

    private void Update()
    {
        UpdateHover();
        if (currentHover != null && Input.GetKeyDown(interactKey))
        {
            OnPressed?.Invoke(currentHover);
        }
    }

    private void UpdateHover()
    {
        Inspectable next = null;
        if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit, maxDistance, inspectableLayer))
        {
            next = hit.collider.GetComponentInParent<Inspectable>();
            if (next != null && next.info == null)
            {
                next = null;
            }
        }

        if (next == currentHover) return;

        if (currentHover != null) currentHover.SetHighlight(false);
        if (next != null) next.SetHighlight(true);
        currentHover = next;
    }

    private void OnDisable()
    {
        if (currentHover != null)
        {
            currentHover.SetHighlight(false);
            currentHover = null;
        }
    }
}
```

- [ ] **Step 5.2: Confirmar compilação**

Console do Unity sem erros vermelhos.

- [ ] **Step 5.3: Commit (pular se for não commitar)**

```bash
git add Assets/Scripts/Interaction/InteractionRaycaster.cs
git commit -m "feat: add InteractionRaycaster with hover/press events"
```

---

## Task 6: `InspectionController` MonoBehaviour

**Files:**
- Create: `Assets/Scripts/Interaction/InspectionController.cs`

- [ ] **Step 6.1: Criar `Assets/Scripts/Interaction/InspectionController.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class InspectionController : MonoBehaviour
{
    public Transform holdAnchor;
    public float rotationSpeed = 200f;
    public PlayerController playerController;
    public InspectionUI ui;
    public InteractionRaycaster raycaster;
    public KeyCode exitKey = KeyCode.Escape;

    private enum State { Idle, Inspecting }
    private State state = State.Idle;

    private Inspectable currentTarget;
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private bool originalRigidbodyKinematic;
    private Rigidbody targetRigidbody;
    private readonly List<Collider> disabledColliders = new List<Collider>();

    private void Awake()
    {
        Assert.IsNotNull(holdAnchor, "InspectionController: holdAnchor não atribuído.");
        Assert.IsNotNull(playerController, "InspectionController: playerController não atribuído.");
        Assert.IsNotNull(ui, "InspectionController: ui não atribuído.");
        Assert.IsNotNull(raycaster, "InspectionController: raycaster não atribuído.");
    }

    private void OnEnable()
    {
        raycaster.OnPressed += HandlePressed;
    }

    private void OnDisable()
    {
        raycaster.OnPressed -= HandlePressed;
    }

    private void HandlePressed(Inspectable target)
    {
        if (state != State.Idle) return;
        Enter(target);
    }

    private void Update()
    {
        if (state != State.Inspecting) return;

        float yaw = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float pitch = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
        currentTarget.transform.Rotate(Vector3.up, -yaw, Space.World);
        currentTarget.transform.Rotate(holdAnchor.right, pitch, Space.World);

        if (Input.GetKeyDown(raycaster.interactKey) || Input.GetKeyDown(exitKey))
        {
            Exit();
        }
    }

    private void Enter(Inspectable target)
    {
        currentTarget = target;
        currentTarget.SetHighlight(false);

        originalParent = target.transform.parent;
        originalLocalPos = target.transform.localPosition;
        originalLocalRot = target.transform.localRotation;

        targetRigidbody = target.GetComponent<Rigidbody>();
        if (targetRigidbody != null)
        {
            originalRigidbodyKinematic = targetRigidbody.isKinematic;
            targetRigidbody.isKinematic = true;
        }

        disabledColliders.Clear();
        foreach (var c in target.GetComponentsInChildren<Collider>())
        {
            if (c.enabled)
            {
                c.enabled = false;
                disabledColliders.Add(c);
            }
        }

        target.transform.SetParent(holdAnchor, worldPositionStays: false);
        target.transform.localPosition = Vector3.zero;
        target.transform.localRotation = Quaternion.identity;

        playerController.enabled = false;
        raycaster.enabled = false;

        ui.Show(target.info);
        state = State.Inspecting;
    }

    private void Exit()
    {
        ui.Hide();

        currentTarget.transform.SetParent(originalParent, worldPositionStays: false);
        currentTarget.transform.localPosition = originalLocalPos;
        currentTarget.transform.localRotation = originalLocalRot;

        if (targetRigidbody != null)
        {
            targetRigidbody.isKinematic = originalRigidbodyKinematic;
        }

        foreach (var c in disabledColliders)
        {
            if (c != null) c.enabled = true;
        }
        disabledColliders.Clear();

        playerController.enabled = true;
        raycaster.enabled = true;

        currentTarget = null;
        targetRigidbody = null;
        state = State.Idle;
    }
}
```

- [ ] **Step 6.2: Confirmar compilação**

Console do Unity sem erros vermelhos.

- [ ] **Step 6.3: Commit (pular se for não commitar)**

```bash
git add Assets/Scripts/Interaction/InspectionController.cs
git commit -m "feat: add InspectionController orchestrating pickup and UI"
```

---

## Task 7: Criar layer `Inspectable` e material de outline

Passos manuais no Editor Unity.

**Files:**
- Modify: `ProjectSettings/TagManager.asset` (via Editor)
- Create: `Assets/Materials/Outline.mat`

- [ ] **Step 7.1: Criar layer `Inspectable`**

No Unity: *Edit → Project Settings → Tags and Layers*. Em *Layers*, encontrar o primeiro slot vazio (geralmente User Layer 6 ou superior). Digitar `Inspectable`. Salvar.

- [ ] **Step 7.2: Criar `Assets/Materials/Outline.mat`**

No Project window: clicar com botão direito em `Assets/Materials` → *Create → Material*. Renomear para `Outline`.

Configurar:
- Shader: *Universal Render Pipeline/Unlit* (se for URP) ou *Unlit/Color* (se for Built-in).
- Cor: ciano claro `(0, 1, 1, 1)` ou amarelo `(1, 0.9, 0, 1)` — escolher uma cor que destaque.
- *Render Face*: `Front` (no Unlit do URP) ou via shader Cull Front em Built-in. **Importante:** este material é renderizado por cima/em volta do objeto; uma alternativa simples é só usar Unlit colorido — o efeito não será um outline verdadeiro mas indica seleção. Para outline real, baixar um shader gratuito como `Quick Outline` do Asset Store ou usar URP Renderer Feature. Para o primeiro corte, Unlit colorido como segundo material já é suficiente para feedback.

> Se preferir um outline real, substituir `Outline.mat` por um material com shader específico (ex.: "OutlineSurfaceShader" do GitHub `chrisnolet/QuickOutline`). O sistema funciona com qualquer material extra atribuído a `outlineMaterial` no `Inspectable`.

- [ ] **Step 7.3: Commit (pular se for não commitar)**

```bash
git add ProjectSettings/TagManager.asset Assets/Materials/Outline.mat Assets/Materials/Outline.mat.meta
git commit -m "chore: add Inspectable layer and outline material"
```

---

## Task 8: Setup do Player na cena

Passos manuais no Editor com a `MainScene` aberta.

**Files:**
- Modify: `Assets/Scenes/MainScene.unity` (via Editor)

- [ ] **Step 8.1: Adicionar `HoldAnchor` como filho da Main Camera**

Na hierarquia: selecionar `Player → Main Camera`. Botão direito → *Create Empty*. Renomear para `HoldAnchor`. No Inspector, *Transform*:
- Position: `(0, -0.15, 0.5)`
- Rotation: `(0, 0, 0)`
- Scale: `(1, 1, 1)`

- [ ] **Step 8.2: Adicionar `InteractionRaycaster` ao Player**

Selecionar `Player` na hierarquia. *Inspector → Add Component → InteractionRaycaster*. Configurar:
- *Ray Origin*: arrastar `Main Camera` (filha do Player).
- *Max Distance*: `2.5`
- *Inspectable Layer*: marcar `Inspectable`.
- *Interact Key*: `E`

- [ ] **Step 8.3: Adicionar `InspectionController` ao Player**

Selecionar `Player`. *Add Component → InspectionController*. Configurar:
- *Hold Anchor*: arrastar o `HoldAnchor` criado.
- *Rotation Speed*: `200`
- *Player Controller*: arrastar o próprio Player (componente PlayerController).
- *Ui*: deixar vazio por enquanto (Task 9).
- *Raycaster*: arrastar o próprio Player (componente InteractionRaycaster).
- *Exit Key*: `Escape`

- [ ] **Step 8.4: Salvar a cena**

*File → Save* (Ctrl+S).

- [ ] **Step 8.5: Commit (pular se for não commitar)**

```bash
git add Assets/Scenes/MainScene.unity
git commit -m "feat: wire Raycaster, InspectionController and HoldAnchor on Player"
```

---

## Task 9: Setup do Canvas e UI

Passos manuais no Editor.

**Files:**
- Modify: `Assets/Scenes/MainScene.unity` (via Editor)

- [ ] **Step 9.1: Criar Canvas**

Hierarquia: botão direito → *UI → Canvas*. Renomear para `InspectionCanvas`.

No Canvas:
- *Render Mode*: `Screen Space - Overlay`
- *UI Scale Mode* (no Canvas Scaler): `Scale With Screen Size`, reference resolution `1920 x 1080`, match `0.5`.

- [ ] **Step 9.2: Criar painel lateral**

Filho do Canvas: *UI → Panel*. Renomear para `InspectionPanel`. No RectTransform:
- *Anchor preset*: `right - stretch` (clicar no botão de anchor segurando Alt+Shift para alinhar).
- *Width*: `400`
- *Left*: `-400`, *Right*: `0`, *Top*: `0`, *Bottom*: `0` (ocupa lado direito inteiro).
- *Image Color*: preto com alpha `0.6` (semi-transparente).

Desativar o painel: no Inspector, desmarcar o checkbox ao lado do nome do GameObject.

- [ ] **Step 9.3: Criar `NameText` dentro do painel**

Filho de `InspectionPanel`: *UI → Text - TextMeshPro* (aceitar o import do TMP Essentials se aparecer). Renomear para `NameText`.

- *RectTransform*: anchor top-stretch, height `60`, top `20`, left `20`, right `20`.
- *Font Size*: `36`
- *Font Style*: Bold
- *Alignment*: Left, Top
- *Text*: deixar vazio ou "Nome".

- [ ] **Step 9.4: Criar `DescriptionText` dentro do painel**

Filho de `InspectionPanel`: *UI → Text - TextMeshPro*. Renomear para `DescriptionText`.

- *RectTransform*: anchor stretch-stretch. *Top*: `100`, *Bottom*: `60`, *Left*: `20`, *Right*: `20`.
- *Font Size*: `20`
- *Alignment*: Left, Top
- *Enable Word Wrap*: marcado.
- *Text*: deixar vazio.

- [ ] **Step 9.5: Criar dica de teclas no rodapé do painel**

Filho de `InspectionPanel`: *UI → Text - TextMeshPro*. Renomear para `HintText`.

- *Anchor*: bottom-stretch, height `40`, bottom `10`, left `20`, right `20`.
- *Font Size*: `16`
- *Alignment*: Center
- *Text*: `[E] devolver   [Mouse] girar`

- [ ] **Step 9.6: Adicionar `InspectionUI` ao Canvas**

Selecionar `InspectionCanvas`. *Add Component → InspectionUI*. Configurar:
- *Name Text*: arrastar `NameText`.
- *Description Text*: arrastar `DescriptionText`.
- *Panel*: arrastar `InspectionPanel`.

- [ ] **Step 9.7: Conectar `InspectionUI` ao `InspectionController`**

Selecionar `Player`. No componente `InspectionController`, campo *Ui*: arrastar `InspectionCanvas`.

- [ ] **Step 9.8: Salvar e commitar**

*File → Save* (Ctrl+S).

```bash
git add Assets/Scenes/MainScene.unity
git commit -m "feat: add InspectionCanvas with side panel and TMP texts"
```

---

## Task 10: Criar os 6 assets de `GlasswareInfo`

Passos manuais no Editor. Conteúdo de cada texto vem abaixo.

**Files:**
- Create: `Assets/Data/Glassware/Beaker.asset`
- Create: `Assets/Data/Glassware/Erlenmeyer.asset`
- Create: `Assets/Data/Glassware/TestTube.asset`
- Create: `Assets/Data/Glassware/GraduatedCylinder.asset`
- Create: `Assets/Data/Glassware/FlorenceFlask.asset`
- Create: `Assets/Data/Glassware/SpiritLamp.asset`

- [ ] **Step 10.1: Criar a pasta**

Project window: botão direito em `Assets` → *Create → Folder*, nome `Data`. Dentro de `Data`: criar pasta `Glassware`.

- [ ] **Step 10.2: Criar `Beaker.asset`**

Em `Assets/Data/Glassware`: botão direito → *Create → Virtual Lab → Glassware Info*. Renomear para `Beaker`. Preencher no Inspector:

- *Display Name*: `Béquer`
- *Description*:
```
Recipiente cilíndrico de vidro borossilicato com bico, usado para misturar, aquecer e medir aproximadamente o volume de líquidos. A graduação impressa é apenas estimativa — para medidas precisas usa-se a proveta. Resiste a choque térmico, podendo ser aquecido diretamente sobre tela de amianto.
```

- [ ] **Step 10.3: Criar `Erlenmeyer.asset`**

- *Display Name*: `Erlenmeyer`
- *Description*:
```
Frasco cônico com base larga e gargalo estreito, ideal para titulações e reações que envolvem agitação manual, pois o formato evita respingos. Também usado para aquecer líquidos e armazenar soluções temporariamente. Recebe esse nome em homenagem ao químico alemão Emil Erlenmeyer.
```

- [ ] **Step 10.4: Criar `TestTube.asset`**

- *Display Name*: `Tubo de Ensaio`
- *Description*:
```
Tubo cilíndrico fechado em uma extremidade, usado para realizar reações em pequena escala, observar precipitados e aquecer pequenas quantidades de substâncias. Sempre manipulado com pinça quando aquecido e direcionado para longe do operador e dos colegas.
```

- [ ] **Step 10.5: Criar `GraduatedCylinder.asset`**

- *Display Name*: `Proveta`
- *Description*:
```
Cilindro graduado de vidro ou plástico usado para medir volumes de líquidos com precisão muito superior à de um béquer. A leitura é feita pela parte inferior do menisco, com o olho na altura do líquido. Não deve ser aquecida nem usada em reações químicas — função estritamente volumétrica.
```

- [ ] **Step 10.6: Criar `FlorenceFlask.asset`**

- *Display Name*: `Balão de Florence`
- *Description*:
```
Balão de fundo redondo e gargalo longo, projetado para aquecer líquidos de forma uniforme — o formato esférico distribui melhor o calor do que recipientes de fundo plano. Muito usado em destilações simples e na preparação de soluções aquecidas. Sempre apoiado em suporte com argola e tela de amianto.
```

- [ ] **Step 10.7: Criar `SpiritLamp.asset`**

- *Display Name*: `Lamparina a Álcool`
- *Description*:
```
Fonte de chama portátil que queima álcool etílico através de um pavio de algodão, usada para aquecer pequenos volumes em tubos de ensaio e cápsulas. Atinge cerca de 700 °C — suficiente para a maioria das reações didáticas, mas insuficiente para fundir vidro ou metais. Apagar sempre cobrindo com a tampa, nunca soprando.
```

- [ ] **Step 10.8: Commit (pular se for não commitar)**

```bash
git add Assets/Data
git commit -m "feat: add 6 GlasswareInfo assets with PT-BR descriptions"
```

---

## Task 11: Configurar os 6 prefabs com `Inspectable`

Passos manuais. Cada prefab fica em `Assets/ThidParty/3D Laboratory Environment with Appratus/Prefabs/`.

| GlasswareInfo | Prefab |
|---|---|
| Beaker | `Beaker.prefab` |
| Erlenmeyer | `Erlenmeyer_flask.prefab` |
| TestTube | `Glass_Lab_test_tube.prefab` |
| GraduatedCylinder | `Graduated_Cylinder.prefab` |
| FlorenceFlask | `florence_flask.prefab` |
| SpiritLamp | `Spirit_Lamp with water.prefab` |

**Files:**
- Modify: cada prefab listado acima (via Editor).

- [ ] **Step 11.1: Para cada prefab, abrir no modo Prefab Edit**

Clicar duas vezes no prefab no Project window para entrar no modo de edição isolado.

- [ ] **Step 11.2: Atribuir layer `Inspectable` na raiz**

Selecionar o GameObject raiz. No Inspector, topo: *Layer* → `Inspectable`. Se Unity perguntar "Change children too?", responder **Yes**.

- [ ] **Step 11.3: Confirmar que tem `Collider`**

No Inspector, verificar a presença de algum `Collider` (MeshCollider, BoxCollider, etc.). Se não tiver, adicionar *Add Component → Box Collider* e ajustar tamanho ao mesh.

- [ ] **Step 11.4: Adicionar componente `Inspectable`**

*Add Component → Inspectable*. Preencher:
- *Info*: arrastar o `GlasswareInfo` correspondente da pasta `Assets/Data/Glassware/`.
- *Target Renderer*: arrastar o `MeshRenderer` principal do prefab (se houver vários, escolher o do corpo da vidraria).
- *Outline Material*: arrastar `Assets/Materials/Outline.mat`.

- [ ] **Step 11.5: Sair do modo Prefab e salvar**

Clicar na seta `<` no topo da hierarquia para sair. Confirmar *Save* se pedir.

Repetir 11.1–11.5 para os 6 prefabs da tabela.

- [ ] **Step 11.6: Commit (pular se for não commitar)**

```bash
git add "Assets/ThidParty/3D Laboratory Environment with Appratus/Prefabs"
git commit -m "feat: configure 6 glassware prefabs with Inspectable component"
```

---

## Task 12: Posicionar as vidrarias na MainScene

Passos manuais com `MainScene` aberta.

**Files:**
- Modify: `Assets/Scenes/MainScene.unity` (via Editor)

- [ ] **Step 12.1: Arrastar cada prefab para a cena**

Da Project window, arrastar para a Scene view (em cima das mesas existentes na MainRoom):
- `Beaker.prefab`
- `Erlenmeyer_flask.prefab`
- `Glass_Lab_test_tube.prefab`
- `Graduated_Cylinder.prefab`
- `florence_flask.prefab`
- `Spirit_Lamp with water.prefab`

- [ ] **Step 12.2: Ajustar posições**

Posicionar de modo que o Player (em `(-5.5, 1, -0.19)`) consiga chegar perto e mirar a cada uma. Espaçar as 6 vidrarias em cima das mesas com folga de ~40 cm entre elas. Garantir que estão apoiadas na superfície (Y compatível com a altura da mesa).

- [ ] **Step 12.3: Salvar a cena**

*File → Save* (Ctrl+S).

- [ ] **Step 12.4: Commit (pular se for não commitar)**

```bash
git add Assets/Scenes/MainScene.unity
git commit -m "feat: place 6 glassware instances in MainScene"
```

---

## Task 13: Validação manual (playtest)

Nenhuma alteração de arquivo. Roteiro de verificação no Editor.

- [ ] **Step 13.1: Play Mode**

Clicar no botão *Play* no Editor.

- [ ] **Step 13.2: Hover**

Andar até perto do Béquer (W/A/S/D + mouse look). Mirar com a cruz central no objeto.
**Esperado:** o béquer ganha o outline (cor do `Outline.mat`).

Desmirar (girar a câmera para fora).
**Esperado:** outline desaparece imediatamente.

- [ ] **Step 13.3: Pegar**

Mirar no béquer e apertar `E`.
**Esperado:**
- Béquer "salta" pra frente da câmera (posição do HoldAnchor).
- Painel lateral direito aparece com "Béquer" e a descrição.
- Movimento WASD não funciona, mouse look não gira a câmera.

- [ ] **Step 13.4: Girar**

Mover o mouse.
**Esperado:** o béquer rotaciona no eixo Y (mouse X) e no eixo X (mouse Y). Sem travamento de pitch.

- [ ] **Step 13.5: Devolver**

Apertar `E`.
**Esperado:**
- Painel some.
- Béquer volta exatamente para a posição original (em cima da mesa, mesma rotação).
- Movimento e câmera voltam a funcionar.

- [ ] **Step 13.6: Repetir com outra vidraria**

Andar até o Erlenmeyer. Inspecionar. Devolver. Deve funcionar do mesmo jeito, com texto diferente no painel.

- [ ] **Step 13.7: Caso negativo**

Mirar numa parede ou na mesa. Apertar `E`.
**Esperado:** nada acontece. Sem outline, sem painel.

- [ ] **Step 13.8: ESC**

Pegar uma vidraria. Apertar `Escape`.
**Esperado:** devolve igual ao E.

- [ ] **Step 13.9: Parar Play Mode**

Apertar *Play* de novo para sair.

Se algum dos passos 13.2–13.8 falhar, voltar ao passo correspondente do plano e revisar a configuração.

- [ ] **Step 13.10: Commit final (pular se for não commitar)**

Nada de código a commitar, mas se houver alguma cena/asset modificado durante o playtest (ex.: posições ajustadas), commitar:

```bash
git status
git add Assets/Scenes/MainScene.unity
git commit -m "chore: adjust glassware positions after playtest"
```

---

## Spec coverage (self-check)

| Requisito do spec | Task |
|---|---|
| `GlasswareInfo` ScriptableObject | Task 2 |
| `Inspectable` MonoBehaviour + outline via material extra | Task 4, 7, 11 |
| `InteractionRaycaster` com raycast, hover, evento OnPressed | Task 5 |
| `InspectionController` state machine, reparenta no HoldAnchor, trava PlayerController | Task 6, 8 |
| `InspectionUI` com Show/Hide e TMP | Task 3, 9 |
| Layer `Inspectable` filtrando raycast | Task 7, 11 |
| Material de outline compartilhado | Task 7, 11 |
| 6 vidrarias (Béquer, Erlenmeyer, Tubo, Proveta, Florence, Lamparina) com texto PT-BR | Task 10, 11, 12 |
| Saída por E ou ESC | Task 6, 13 |
| Rigidbody/colliders restaurados na saída | Task 6 |
| EditMode tests para `GlasswareInfo` e `InspectionUI` | Task 2, 3 |
| Validação manual dos casos do spec | Task 13 |

Todos os itens do spec estão cobertos por ao menos uma task.
