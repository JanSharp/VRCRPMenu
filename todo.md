
- [x] Show by permission scripts in players backend page
- [x] Change how the odd and even row images get enabled and disabled
- [x] Prevent changing the last player which has permissions to edit permissions to a group which does not have those permissions
- [x] Use ~~a selectable or a toggle~~ CrossFadeAlpha for the highlight shown while the permission group popup is shown
- [x] When losing permissions to edit one of the columns in the player backend, but that was the active sort order, change sort order to the player name
- [ ] Maybe actually just disable (make non interactable) the 3 columns that are tied to permissions in the players backend page rather than hiding them, except for the delete column that would go hidden
- [ ] Should there be an indicator for when a player is online or offline in the backend page outside of the delete button being greyed out?
- [x] Check permissions for all interactions in the players backend manager, when running the IAs. Raise events in case an action got ignored so external latency states can be reset
- [x] reduce the amount of raycast targets
- [x] experiment with sub canvases? but idk how they would even help with scrolling requiring layout changes? Unless the entire content can be a sub canvas? Experiment. - See [ui-performance](ui-performance.md)
- [x] genuinely consider making custom layout elements... why the actual hell is that something I have to consider? Like how. I just don't understand how Unity's components can be so terrible. Or are they? Apparently I cannot trust whatever people say on the internet about UI's anyway because Canvas Groups most certainly have measurable - noticeable even - performance impact when used to disable and hide UI. But if layout groups really are using GetComponent every time, and I can actually look at source code, probably, to double check that myself, then yea actually horrifying
  - Don't. At most make "build time layout groups" for any static layout. They only affect performance when layout elements have to mark layout systems as dirty, which is primarily only problematic when enabling or disabling large hierarchies. Only bother optimizing this if there are noticeable and bothersome lag spikes
- [x] test having 89 players and then importing the 4 players again, idk how 2 out of 4 of the show by permission resolvers ended up not working for just 1 row in that case. very random
  - I tried, could not reproduce. And it won't matter for this case again once all of those scripts got removed in favor of the page script handling all of the enabling and disabling
- [x] most likely remove all of the show by permission scripts from the rows and instead have the page script handle the enabling and disabling. It's just all around more performant that way, both creation of rows as well as changing permissions
- [x] experiment with enabling and disabling elements as you scroll through the list, only having the visible ones enabled basically
- [x] ~~do not use auto size unless necessary~~ sure, but don't fret it too much. It would only reduce lag spikes such as when changing permissions, outside of that it hardly matters which causes layout rebuilds which changes the size of text rects which make text rebuild
- [x] could disable all Caret game objects as they are useless in VRChat and just eat cull performance
  - find them after instantiating a row, since they get created OnEnable and don't exist at build time (or scene load generically)
- [x] test if disabling the root canvas stops all performance drain including moving - it does not, culling cost persists
- [x] test if moving causes layout set dirty calls made by images and text (graphics) - it does not
- [x] clamp the velocity of the scroll rect, even though it only matters for desktop since in VR it's constantly rebuilding batches anyway
- [x] highlight the row that is about to be deleted behind the popup
- [x] auto delete old read GM requests
- [x] respect permissions in GM requests manager
- [x] maybe do raise the latency events alongside the game state events in GM requests manager, making listening to just latency events inform a script about all actions taken, without needing to listen to the game state events too
- [x] move the show page by permission script into the gm requests buttons module
- [x] why are the request gm buttons no longer visible after making those scripts themselves also be permission resolvers, thus having 2 resolvers on the same object? What is going on - a permission system race condition that got introduced due to lockstep on init now being able to be spread out across frames
- [x] importing and opening the players backend page leaves rows disabled sometimes, like it didn't recalculate which ones should be visible
- [x] check every frame if the viewport rect height has changed, which also removes the hack of setting the scroll rect position to 1 initially
- [x] update which rows are visible every time one gets added or removed... but like isn't it already doing that? why was a row invisible after others got deleted? And it didn't even show up when scrolling?! - probably most likely fixed, not sure if the observation here of it not updating even after scrolling was correct
- [x] update names in the gm requests list whenever overridden display name or character names change
- [x] make urgent highlight image not fade in and out, it changing position instantly and then playing the fade looks stupid, just how the toggle checkmark looked stupid
- [x] add a main highlight color image for active gm requests
- [x] remove the canvas group, keeping read gm requests the same full opacity, just with the urgent or main color highlights disabled
- [x] sort read requests the other way around timing wise, newest to the top
- [x] why do the gm request toggles look like they have a different color than the page toggles?
- [x] make regular gm requests red after x minutes
- [ ] try out non elastic scroll rects
- [x] update gm requests names upon import
- [x] make buttons aware of all open requests by the local player
- [x] hud for the gm requests
- [x] do something with gm requests where the requesting player left, either visually or idk
- [x] deleting the second last row makes the last row invisible until that row goes out of view and into view again
- [x] rename the RequestGMButtons prefab to GMRequestButtons
- [ ] very most likely change gm requests page to make calls to an external teleports manager
- [ ] maybe, probably, make the count slightly smaller for GM requests HUD to make it feel less squished into that spot. Though would be good to test in VR first
- [ ] add the crazy ideas for requesting GM without opening the menu, see gm-menu-requirements.md in menu-system
- [x] How to truly prevent locking yourself out of permissions to edit permissions
  - editing the permission values themselves could lock you out
    - [x] disallow removing the edit permissions permission if the actively edited group is the group you yourself are in
  - deleting a permission group could lock you out
    - [x] disallow deleting the permission group you are in if the default group does not have the edit permissions permission
  - deleting the last player data which had the necessary permissions could lock you out
    - [x] disallow deleting the last offline player data which is in a group that has the edit permissions permission
  - changing the last player data's group could lock you out
    - [x] disallow changing your permission group to a group which does not have the edit permissions permission
- [x] think about combining the edit permissions permission and the change permission group permission
- [x] voice range settings UI
- [x] voice range HUD
- [x] voice range in world indicator
- [x] voice range AudioManager integration
- [x] which ones of these layout elements actually need layout priority 2? something with the input fields afaik
- [x] change GMRequestsHUD to use Time.deltaTime instead
- [x] make default visualization type for hud and in world defined through inspector
- [ ] maybe some kind of flash or indicator on the hud for when a new GM request came in. Or like a plus sign that pops up from the hud and moves upwards before fading away
- [x] show mask settings toggle group sets flags for any definitions that do not have a toggle to off rather than using the appropriate default value
- [x] tweak voice range shader properties or shader itself, likely to make it less obtrusive
- [x] getting shader compile error on publish:
```
Shader error in 'RP Menu/Voice Range Sphere': undeclared identifier 'samplerdepthTexture' at line 136 (on d3d11)

Compiling Subshader: 0, Pass: , Vertex program with STEREO_INSTANCING_ON
Platform defines: SHADER_API_DESKTOP UNITY_ENABLE_DETAIL_NORMALMAP UNITY_ENABLE_REFLECTION_BUFFERS UNITY_LIGHTMAP_FULL_HDR UNITY_LIGHT_PROBE_PROXY_VOLUME UNITY_PBS_USE_BRDF1 UNITY_SPECCUBE_BLENDING UNITY_SPECCUBE_BOX_PROJECTION UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS
Disabled keywords: INSTANCING_ON SHADER_API_GLES30 UNITY_ASTC_NORMALMAP_ENCODING UNITY_COLORSPACE_GAMMA UNITY_FRAMEBUFFER_FETCH_AVAILABLE UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS UNITY_HARDWARE_TIER1 UNITY_HARDWARE_TIER2 UNITY_HARDWARE_TIER3 UNITY_LIGHTMAP_DLDR_ENCODING UNITY_LIGHTMAP_RGBM_ENCODING UNITY_METAL_SHADOWS_USE_POINT_FILTERING UNITY_NO_DXT5nm UNITY_NO_FULL_STANDARD_SHADER UNITY_NO_SCREENSPACE_SHADOWS UNITY_PBS_USE_BRDF2 UNITY_PBS_USE_BRDF3 UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION UNITY_UNIFIED_SHADER_PRECISION_MODEL UNITY_VIRTUAL_TEXTURING
UnityEditor.BuildPipeline:BuildAssetBundles (string,UnityEditor.AssetBundleBuild[],UnityEditor.BuildAssetBundleOptions,UnityEditor.BuildTarget)
VRC.SDK3.Editor.Builder.VRCWorldAssetExporter:ExportCurrentSceneResource (bool,System.Action`1<string>,System.Action`1<object>)
VRC.SDK3.Editor.Builder.VRCWorldBuilder:ExportSceneResourceInternal (bool)
VRC.SDK3.Editor.Builder.VRCWorldBuilder:ExportSceneResource ()
VRC.SDK3.Editor.VRCSdkControlPanelWorldBuilder/<Build>d__149:MoveNext () (at ./Packages/com.vrchat.worlds/Editor/VRCSDK/SDK3/VRCSdkControlPanelWorldBuilder.cs:2567)
UnityEngine.UnitySynchronizationContext:ExecuteTasks ()
```
- NOTE: HUD text does not draw above opaque geometry while images do. I believe that fixing that would require making a copy of the text mesh pro shaders just to change ZTest to Always, or maybe using legacy text instead. Pretty sure the former would work, though it is very annoying, and did not try the latter
- NOTE: the RPMenu prefab should not contain the generated permission rows already. I mean its fine if it does, but it shouldn't. So when making a release, make sure to hit the build menu button beforehand
- [x] proofread all of the permission page related code
- [x] importing the 1000 players, the loading UI closed too soon, it was still running OnImportFinishingUp. This could be a lockstep with the IsImporting flag or a menu system bug
- [x] the cross fade alpha api for UI graphics is unreliable when objects get disabled. Either don't use it at all - and use like a selectable instead for example - or find all the edge cases and handle them
- [x] check if permission groups are deleted already when getting the created event
- [x] change popup buttons to use the correct layout element style
- [x] change all texts which get dimmed (disabled) through selectables to use the tintable style
- [x] the ShowAdjacentPopups script should not be a permission resolver, it should be other scripts telling it which popups are shown or not. Case and point, there's 2 permissions for each of those popups, local and global, so it cannot even handle the current use case right now
- [ ] maybe close the save selection group popup upon closing the menu in VR
- [ ] maybe close the load selection group popup upon closing the menu in VR, but only if none of the buttons are marked for deletion
- [x] change the postfix for selection groups labels to put the \[G\] before the (#)
- [x] make sure the "no dynamic data" texts have proper styling
- [x] disable load selection button when there are no selection groups
