# Troubleshooting

- **Git UPM cannot find a package:** use the leading-slash `?path=/DevAccelerationSystem/Assets/<package>` URL and a reachable tag or commit SHA.
- **A Unity target cannot compile:** install that target's Unity module; compilation checks are not player builds.
- **OpenUPM install is unavailable:** the packages are not registered yet. Use Git UPM until a package-specific release and OpenUPM submission are authorized.
- **A package test assembly is not visible:** ensure the package was installed as a UPM package and that the Unity Test Framework is present.
- **A logger or localization issue is platform-specific:** reproduce in the tracked demo project and do not treat editor proof as native-device proof.
