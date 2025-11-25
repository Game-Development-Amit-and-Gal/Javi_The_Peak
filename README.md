# JAVI Storybook – Interactive Page-Flip Game

This project is a small Unity game / interactive storybook where the player
“flips” through a virtual book.  
Some of the pages are not just static images – they are live views from Unity
cameras (mini-scenes / small games, like JAVI walking in a 2D world).

The core of the book logic is in `Book.cs`, which controls:

- The **page curl animation**
- The **current page index** (`currentPage`)
- The **mouse drag** used to flip pages
- The **camera pages** (GameObjects that show what a camera sees on a page)

---

## Features

- 📝 **Interactive 2D/3D book** with realistic page-flip (curl) animation  
- 🎥 **Camera pages**:
  - `cameraPage0` – content for page 0
  - `cameraPage1`, `cameraPage2`, `cameraPage3`, `cameraPage4` – content for
    later pages (1–2, 2–3, etc.)  
- 👣 **Character scene** (e.g., JAVI walking) rendered by a camera and shown
  inside the book page
- 🖱️ Mouse-drag interaction: drag the right or left side of the book to flip
  forward/backward
- 💡 Support for both **legacy Input Manager** and **New Input System**  
  (using `ENABLE_INPUT_SYSTEM` / `ENABLE_LEGACY_INPUT_MANAGER` defines)
- 🌗 Optional **shadow** and clipping effects for nicer visual curls

---

## How the Book Works (Book.cs)

### Main components

The `Book` component is attached to a GameObject under a `Canvas`. It references:

- `BookPanel` – the main RectTransform of the book
- `Left`, `Right` – current left/right pages being curled
- `LeftNext`, `RightNext` – the “next” pages that will appear after flipping
- `ClippingPlane`, `NextPageClip` – used for the page-curl effect
- `Shadow`, `ShadowLTR` – visual shadow effects
- `bookPages[]` – array of Sprites for all the pages
- `currentPage` – index of the page shown on the **right** in idle state
- `cameraPage0 .. cameraPage4` – UI GameObjects (usually `RawImage`) that show
  RenderTextures from various cameras

There is also an enum:

```csharp
public enum FlipMode
{
    RightToLeft,
    LeftToRight
}
```

and a flag:

```csharp
bool pageDragging = false;
```

to mark when a flip is in progress.

---

### Input & dragging

The helper method:

```csharp
private Vector3 GetPointerScreenPosition()
```

returns the mouse/touch position using either:

- The **New Input System** (`Mouse.current`, `Touchscreen.current`)
- Or the **old** `Input.mousePosition`

When the user drags:

- `OnMouseDragRightPage()` → calls `DragRightPageToPoint(...)`
- `OnMouseDragLeftPage()` → calls `DragLeftPageToPoint(...)`

These methods:

1. Set `pageDragging = true`
2. Set `mode` to `RightToLeft` or `LeftToRight`
3. Activate / arrange `Left`, `Right`, `LeftNext`, `RightNext`
4. Call `UpdateBookRTLToPoint` / `UpdateBookLTRToPoint` to update the curl

When the user releases the mouse:

```csharp
public void OnMouseRelease()
{
    if (interactable)
        ReleasePage();
}
```

`ReleasePage()` checks where the “corner” `c` ended up relative to
`ebr` (bottom-right) and `ebl` (bottom-left) and decides whether to:

- **TweenBack()** – cancel the flip and go back to the original page
- **TweenForward()** – complete the flip and change `currentPage`

The actual tweening is done in `TweenTo(...)`, which gradually moves the curl
corner and updates the visuals.

---

### Updating the displayed sprites

`UpdateSprites()` decides which Sprites should be on the idle pages:

```csharp
void UpdateSprites()
{
    LeftNext.sprite  = (currentPage > 0 && currentPage <= bookPages.Length)
        ? bookPages[currentPage - 1]
        : background;

    RightNext.sprite = (currentPage >= 0 && currentPage < bookPages.Length)
        ? bookPages[currentPage]
        : background;
}
```

After a successful flip, `Flip()` updates `currentPage` by ±2, resets parents,
hides `Left` and `Right`, and calls `UpdateSprites()` and `UpdateCameraPage()`.

---

## Camera Pages Logic

The **camera pages** are handled in:

```csharp
void UpdateCameraPage()
```

This method:

1. Returns early if there are no `bookPages`.
2. Turns **off** all camera page objects:
   ```csharp
   if (cameraPage0 != null) cameraPage0.SetActive(false);
   if (cameraPage1 != null) cameraPage1.SetActive(false);
   if (cameraPage2 != null) cameraPage2.SetActive(false);
   if (cameraPage3 != null) cameraPage3.SetActive(false);
   if (cameraPage4 != null) cameraPage4.SetActive(false);
   ```
3. Checks the current page layout (`Left`, `Right`, `LeftNext`, `RightNext`
   sprites) and, depending on which page indices are visible (`bookPages[0]`,
   `[1]`, `[2]`, `[3]`), attaches the relevant cameraPage GameObjects to the
   correct page Images.

For example, when the book is idle on page 0:

```csharp
if (RightNext != null && RightNext.sprite == bookPages[0])
{
    AttachCameraTo(RightNext, cameraPage0);
    return;
}
```

The helper:

```csharp
void AttachCameraTo(Image page, GameObject camPage)
{
    if (camPage == null || page == null) return;

    var t = camPage.transform as RectTransform;

    t.SetParent(page.transform, false);
    t.anchorMin = Vector2.zero;
    t.anchorMax = Vector2.one;
    t.offsetMin = Vector2.zero;
    t.offsetMax = Vector2.zero;
    t.localScale = Vector3.one;
    t.localRotation = Quaternion.identity;

    camPage.SetActive(true);
}
```

- Parents the `camPage` UI (for example, a `RawImage` that shows a
  RenderTexture from `Camera_0`, `Camera_12`, etc.) under the page.
- Stretches it to fill the page entirely.
- Activates the GameObject so the scene from that camera becomes visible.

---

## How to Add/Change a Camera Page

1. Create a **Camera** in the scene.
2. Create a **RenderTexture** and assign it to the camera’s `Target Texture`.
3. Create a **UI GameObject** (e.g., `RawImage`) under the Canvas:
   - Assign the RenderTexture to the `Texture` field of the `RawImage`.
4. Drag that GameObject into one of the fields on `Book`:
   - `cameraPage0`
   - `cameraPage1`
   - `cameraPage2`
   - `cameraPage3`
   - `cameraPage4`
5. Adjust `UpdateCameraPage()` logic if you want to control exactly **which
   page index** maps to which camera page.

---

## Controls

- **Flip right** (forward):  
  Drag the right page area (`Right` / `RightNext`) with the mouse.
- **Flip left** (backward):  
  Drag the left page area (`Left` / `LeftNext`).

The drag is usually hooked by pointer/collider events or UI events on the page
Images, which in turn call `OnMouseDragRightPage`, `OnMouseDragLeftPage`,
and `OnMouseRelease`.

---

## Requirements & Setup

- Unity (tested with recent versions that support both legacy Input and the
  New Input System)
- A Canvas in Screen Space – Camera or Overlay
- The `Book` GameObject (with `Book.cs` attached) as a child of the Canvas
- Correctly assigned references in the Inspector:
  - `BookPanel`
  - `Left`, `Right`, `LeftNext`, `RightNext`
  - `ClippingPlane`, `NextPageClip`, `Shadow`, `ShadowLTR`
  - `bookPages` array with all page Sprites
  - `cameraPage0 .. cameraPage4` (optional, if using camera content)

If using the **New Input System**, make sure the appropriate scripting define
symbols (`ENABLE_INPUT_SYSTEM`, `ENABLE_LEGACY_INPUT_MANAGER`) and packages
are set up.

---

## Source of the Book Page Curl Code

The page-flip / page-curl implementation (the base logic of `Book.cs`) is
adapted from the following Unity Asset Store package:

> **Book Page Curl**  
> Unity Asset Store:  
> https://assetstore.unity.com/packages/tools/animation/book-page-curl-55588?aid=1101ldiAE

The original implementation is by the asset’s author.  
In this project, the code was customized and extended to:

- Support multiple **camera pages** (`cameraPage0 .. cameraPage4`)
- Integrate with custom game scenes (like JAVI walking)
- Work with both old and new Unity Input Systems
- Handle custom conditions for when a camera should appear on each page

Please respect the original asset’s license if you clone or reuse this project.

---

## Possible Extensions

- Add more pages and cameras (for example, mini-games per page)
- Add buttons (Next / Previous) in addition to mouse dragging
- Add sounds for page flips
- Connect JAVI’s movement and interactions to specific book pages


  [Game's Link](https://amit-and-gal.itch.io/javi-the-peak)
