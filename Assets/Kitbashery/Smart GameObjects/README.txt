Smart GameObjects Copyright(c) 2023 Kitbashery. All rights reserved.

License Agreement: https://unity.com/legal/as-terms
------------------------------------------------------------------------------------------------------------------------------------------

Thank you for purchasing Smart GameObjects!


If you find you need support you have a few options:

1) Built-in documentation can be accessed by clicking the (?) button below the component header.
This will display an overview of what things do. You may also hover over properties to display specific information as tooltips.
------------------------------------------------------------------------------------------------------------------------------------------

2) Want to speak with other users? Check out Kitbashery's official discord server: https://discord.gg/3vjtMneeFv
------------------------------------------------------------------------------------------------------------------------------------------

3) The Unity forums are another place to get help. Check out the Smart GameObjects thread here:
 https://forum.unity.com/threads/smart-gameobjects-realtime-visual-scripting-physics-toolkit-procedural-animation.1410153/
------------------------------------------------------------------------------------------------------------------------------------------

4) Submit a bug report/feature request on GitHub: https://github.com/Kitbashery/smart-gameobjects-issue-tracker/issues
------------------------------------------------------------------------------------------------------------------------------------------

5) Email kitbashery@gmail.com and provide your invoice number.
------------------------------------------------------------------------------------------------------------------------------------------

6) API Documentation can be found online @ https://kitbashery.com/docs/smart-gameobjects
Reference for specific components can be accessed by clicking the (?) button of a component's header.
------------------------------------------------------------------------------------------------------------------------------------------


FAQ:

Q) How can I get a behavior to run only once?
A) Disable the behavior by adding the "Disable Behavior" action at the end of your actions and provide the page number of the behavior - 1

Q) How can I create new actions or integrate other assets?
A) Actions are hard-coded. It is recommended to use UnityEvents for that. If you want a new action please submit a feature request.

Q) I get warnings after I compile while in play mode, why?
A) Disable runtime compiling in your editor preferences, compiling in play mode messes with the SmartManager instance.

Q) My Smart Mesh has stretched/wavy UVs, why?
A) This is because of Unity's mesh data structure, there may be overlapping UV triangles.
Seams could be fixed by duplicating vertices along seams. This may be improved in future versions.


------------------------------------------------------------------------------------------------------------------------------------------
Optimization Tips:

Like all visual scripting solutions, the output will be less efficient than hand-crafted code.
To ensure your project runs smoothly take try to follow these guidelines:

1) Limit the use of "Delay Next Action" actions, although optimized these use coroutines which generate garbage.
2) Limit the use of Tweens, the evaluation function of animation curves can generate garbage.
3) Limit the use of UnityEvents these calls use reflection under the hood instead use SmartActions when you can.
4) If you have a consistent FPS at or below 30 you may be able to achieve 30-60 again by enabling FPS throttling.
5) Avoid using distance checks or pathfinding actions without a condition since they can be computationally expensive.
6) Use the SmartManager's object pools to instantiate objects via spawn actions when many duplicate objects are needed.
------------------------------------------------------------------------------------------------------------------------------------------


Like the asset? Many long hours go into developing and maintaining this asset; don't forget to leave the developers an encouraging review!
https://assetstore.unity.com/packages/slug/248930