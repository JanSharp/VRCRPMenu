
# UI Performance

## Unsorted

- canvases always render everything as transparent, even if it is opaque no alpha. Be mindful of overdraw, there's a debug overlay. Yellow means bad
- disabling pixel perfect on a sub canvas is supposed to make building batches faster. I see literally no difference.
- text mesh pro auto size is expensive, but only on when building the text. It does not affect batch build times
  - though making all text rebuild with 80 (so over 500 texts) players backend rows, I don't think I should care all that much:
    - with auto size: 60 ms
    - without auto size: 13 ms
- scrolling in a scroll rect does not cause the layout elements to dirty their layout groups, however it does dirty the canvas, causing batches to be built

## Build Batches

- in the profiler this is actually called UpdateBatches. Canvas.BuildBatches also shows up there but hardly any time is attributed to it
- moving a canvas makes it build batches: https://issuetracker.unity3d.com/issues/canvas-dot-buildbatch-occurs-when-moving-the-parent-game-object-of-a-canvas
- disabled canvas does not build batches
- inactive objects inside of an enabled canvas affect batch build time (0.1 ms with about 80 inactive players backend rows)
- canvas batch building scales O(n * log(n)) with the amount of graphics in the canvas, it does a back to front sort
- changing anything on a graphic, except for color or image fill properties, marks the entire canvas as dirty making it build batches (doesn't matter if the canvas is moving every frame, it's already doing it every frame)
- part of building batches is telling all layout systems part of the canvas to update (? I believe, however removing all layout groups makes zero performance difference for this case)
- part of building batches is RectTransform.SyncTransform, which appears to scale linearly ( O(n) ) with the amount of active transforms in the canvas. (0.4 ms with 2000 active transforms, 140 players backend rows)

## Culling

Every enabled graphic (primarily images and text), regardless of canvas enabled state or canvas group alpha, has a constant culling cost (1.1 ms with 80 active players backend rows).

The only way to get rid of this cost is by disabling the graphic or its game object (or parent of course).

## Layout System

- a layout system is the continuous group of layout groups above a component (in the parents)
  - any change that invalidates the layout of an object make that layout element dirty its layout system which does a get component call on the parent, and continues to do get components on the parents until it either does not find a layout group or hits a root object
    - OnEnable
    - OnDisable
    - Reparenting (both on the source and destination end)
    - OnDidApplyAnimationProperties
    - OnRectTransformDimensionsChanged
    - however all of that only if the layout element is enabled. Reparenting while the layout element is disabled does not dirty the layout system
- image, text, scroll rects (?!), ... are layout elements
- this is the cause of large lag spikes when enabling or disabling a large amount of objects. It is worse the more layout groups there are, or more generically the more objects are inside of layout groups and how many layout groups those layout groups are nested in
- pretty sure the actual layout system doing layout isn't all that expensive. It is just the get component calls part of figuring out what system to dirty that is the expensive part

## Raycasting

- every canvas can have a graphics raycaster (the default canvas comes with this component already attached)
- raycasters scale linearly ( O(n) ) with the amount of raycast targets in the canvas
- graphics such as images and text can be raycast targets, and are by default
- the graphics raycaster access the camera property of the canvas multiple times per frame
  - if the property is not set, when it is null, it accesses `Camera.main`
  - `Camera.main` does `FindObjectWithTag`. While that doesn't scale linearly with the amount of tagged objects, it is still worse than O(1) and unacceptable with basically any amount of tagged objects in the scene.
  - Always set the camera property of canvases
    - [ ] Hopefully VRChat does this, because we do not have access to the main camera. Test this by measuring performance with and without many tagged objects in the scene while having like 100 active canvases
    - [ ] Does setting the property in the inspector affect whether VRChat does or does not do this?
