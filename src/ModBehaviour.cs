/*
 * DuckovBetterActionCancel
 * Copyright (c) 2026 Aile72. All rights reserved.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 *
 * SPDX-License-Identifier: AGPL-3.0-or-later
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace DuckovBetterActionCancel
{
	public class ModBehaviour : Duckov.Modding.ModBehaviour
	{
		private const string HarmonyId = "com.Aile72.DuckovBetterActionCancel";

		private static readonly Type[] PatchClasses =
		{
			typeof(CharacterMainControl_StartAction_Patch),
			typeof(CA_UseItem_SetUseItem_Patch),
			typeof(CharacterMainControl_Interact_Patch),
			typeof(ItemAgentHolder_SetTrigger_Patch),
			typeof(CharacterInputControl_OnReloadInput_Patch),
			typeof(CharacterMainControl_ChangeHoldItem_Patch)
		};

		private Harmony? harmony;

		protected override void OnAfterSetup()
		{
			harmony = new Harmony(HarmonyId);

			CheckGameMembers();

			List<string> failed = new List<string>();

			foreach (Type patchClass in PatchClasses)
			{
				try
				{
					new PatchClassProcessor(harmony, patchClass).Patch();
				}
				catch (Exception exception)
				{
					failed.Add(patchClass.Name);
					ModLog.Error("Patch class failed: " + patchClass.Name + " | " + exception.Message);
				}
			}

			List<string> patched = harmony.GetPatchedMethods()
				.Select(method => (method.DeclaringType?.Name ?? "?") + "." + method.Name)
				.OrderBy(name => name)
				.ToList();

			ModLog.Info("Loaded v" + (info.version ?? "?") + " | Game " + Application.version + " | Patches (" + patched.Count + "/" + PatchClasses.Length + "): " + (patched.Count == 0 ? "none" : string.Join(", ", patched)));

			if (failed.Count > 0)
			{
				ModLog.Error("Patch classes skipped (" + failed.Count + "/" + PatchClasses.Length + "): " + string.Join(", ", failed));
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

		private static void CheckGameMembers()
		{
			CheckMember(AccessTools.Method(typeof(CharacterMainControl), "StartAction"), "CharacterMainControl.StartAction");
			CheckMember(AccessTools.Method(typeof(CA_UseItem), "SetUseItem"), "CA_UseItem.SetUseItem");
			CheckMember(AccessTools.Method(typeof(CharacterMainControl), "Interact", Type.EmptyTypes), "CharacterMainControl.Interact");
			CheckMember(AccessTools.Method(typeof(ItemAgentHolder), "SetTrigger"), "ItemAgentHolder.SetTrigger");
			CheckMember(AccessTools.Method(typeof(CharacterInputControl), "OnReloadInput"), "CharacterInputControl.OnReloadInput");
			CheckMember(AccessTools.Method(typeof(CharacterMainControl), "ChangeHoldItem"), "CharacterMainControl.ChangeHoldItem");
			CheckMember(AccessTools.Field(typeof(CharacterMainControl), "currentAction"), "CharacterMainControl.currentAction");
		}

		private static void CheckMember(MemberInfo? member, string name)
		{
			if (member == null)
			{
				ModLog.Error("Game member missing: " + name + " | the game version may be unsupported");
			}
		}
	}
}
