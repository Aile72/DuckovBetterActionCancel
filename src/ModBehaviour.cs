using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace DuckovBetterActionCancel
{
	public class ModBehaviour : Duckov.Modding.ModBehaviour
	{
		private const string HarmonyId = "com.Aile72.DuckovBetterActionCancel";

		private Harmony? harmony;

		protected override void OnAfterSetup()
		{
			try
			{
				harmony = new Harmony(HarmonyId);
				harmony.PatchAll(typeof(ModBehaviour).Assembly);

				List<string> patched = harmony.GetPatchedMethods()
					.Select(method => (method.DeclaringType?.Name ?? "?") + "." + method.Name)
					.OrderBy(name => name)
					.ToList();

				ModLog.Info("Loaded v" + (info.version ?? "?") + " | patches (" + patched.Count + "): " + (patched.Count == 0 ? "none" : string.Join(", ", patched)));
			}
			catch (Exception exception)
			{
				ModLog.Error("Patching failed: " + exception);
				throw;
			}
		}

		protected override void OnBeforeDeactivate()
		{
			if (harmony == null)
			{
				return;
			}

			try
			{
				harmony.UnpatchAll(HarmonyId);
				ModLog.Info("Unloaded | patches reverted");
			}
			catch (Exception exception)
			{
				ModLog.Error("Unpatching failed: " + exception);
			}
			finally
			{
				harmony = null;
			}
		}
	}
}
