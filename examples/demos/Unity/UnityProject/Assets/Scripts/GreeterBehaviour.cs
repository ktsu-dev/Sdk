namespace Demo.Game;

// Engine-side usage of the plug-in. This file is compiled by Unity, never by the .NET build:
// it lives outside Demo.Unity's project directory, so the SDK's default compile glob does not
// pick it up, and UnityEngine is only on the compile path inside the editor.
using Demo.Unity;
using UnityEngine;

/// <summary>Prints a greeting from the ktsu.Sdk.Unity plug-in in Assets/Plugins.</summary>
public sealed class GreeterBehaviour : MonoBehaviour
{
    private void Start() => Debug.Log(Greeter.Greet("Unity"));
}
