https://github.com/BepInEx/BepInEx.Debug (https://github.com/BepInEx/BepInEx.Debug/releases/latest)

Dynamically reload plugins (.dll) by putting them into `BepInEx/scripts` (doesnt read subdirs)

Plugin class needs `OnDestroy` method:
```cs
[BepInPlugin(ID, NAME, VERSION)]
public class MyPlugin : BaseUnityPlugin
{
	public const string ID = "com.exp111.MyPlugin";
	public const string NAME = "MyPlugin";
	public const string VERSION = "1.0";

	public static ManualLogSource Log;
	private static Harmony harmony;

	void Awake()
	{
		try
		{
			Log = Logger;
			Log.LogMessage("Awake");
			harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ID);
		}
		catch (Exception e)
		{
			Log.LogMessage($"Exception during MyPlugin.Awake: {e}");
		}
	}

	void OnDestroy()
	{
		harmony?.UnpatchSelf();
	}
}
```

Workflow:
Compile in VS
Out folder is symlinked to `BepInEx/scripts` || alternatively put a copy in your post build

batch code for symlink (put in BepInEx/batch):
```batch
pushd "%~dp0/../"
rd scripts
mklink /D scripts "E:\D\Visual Studio\Projects\OutwardMods\MoreChatCommands\thunderstore\plugins"
```
then make a shortcut and always run as admin