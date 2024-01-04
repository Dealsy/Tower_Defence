Thank you for purchasing AnyPath!

Version 1.4

** Upgrade note **
In previous versions, the Path result class had a Segments property. This has been removed
to allow for zero allocation results. The segments of a path can now only be obtained by using the indexed on the class itself.

Check out the demos and read the documentation at: 
https://anypath.bartvandesande.nl

For questions regarding AnyPath feel free to reach out to me at:
anypath@bartvandesande.nl

Installation requirements:
- Unity 2020.3 or higher
- Burst minimum version 1.4.11
- Unity Collections minimum version 0.15.0
If your project uses the Entities package, Unity Collections will already be included

Import AnyPath.unitypackage into your project.
If you encounter any compilation errors, you may need to install these packages manually.

- Click on Window -> Package Manager
- On the packages dropdown, select Packages: Unity Registry
- Locate Burst and install the latest version
- Click on the + > Add package from GIT url
- Enter com.unity.collections and hit ENTER