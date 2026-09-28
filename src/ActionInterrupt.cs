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
using System.Reflection;
using HarmonyLib;
using UnityEngine.InputSystem;

namespace DuckovBetterActionCancel
{
	internal static class ActionInterrupt
	{
		private static FieldInfo? currentActionField;

		private static bool missingFieldReported;

		internal static bool IsPlayerCharacter(CharacterMainControl character)
		{
			if (!character)
			{
				return false;
			}

			LevelManager levelManager = LevelManager.Instance;
			if (!levelManager)
			{
				return false;
			}

			return levelManager.MainCharacter == character || levelManager.ControllingCharacter == character;
		}

		internal static bool CanCancel(CharacterMainControl character)
		{
			if (!IsPlayerCharacter(character))
			{
				return false;
			}

			if (character.isVehicle)
			{
				return false;
			}

			CharacterActionBase current = character.CurrentAction;
			if (!current || !current.Running)
			{
				return false;
			}

			if (current is CA_ControlOtherCharacter)
			{
				return false;
			}

			if (current is CA_Interact interact && interact.InteractingTarget is Duckov.MiniGames.GamingConsole)
			{
				return false;
			}

			if (!current.IsStopable())
			{
				return false;
			}

			if (current is CA_Carry)
			{
				return false;
			}

			return true;
		}

		internal static bool TryCancel(CharacterMainControl character)
		{
			if (!CanCancel(character))
			{
				return false;
			}

			CharacterActionBase current = character.CurrentAction;
			if (!current.StopAction())
			{
				return false;
			}

			ClearCurrentActionRef(character);
			return true;
		}

		internal static void ClearCurrentActionRef(CharacterMainControl character)
		{
			try
			{
				if (currentActionField == null)
				{
					currentActionField = AccessTools.Field(typeof(CharacterMainControl), "currentAction");
				}

				if (currentActionField == null)
				{
					if (!missingFieldReported)
					{
						missingFieldReported = true;
						ModLog.Warn("Field CharacterMainControl.currentAction not found | action reference cannot be cleared");
					}

					return;
				}

				currentActionField.SetValue(character, null);
			}
			catch (Exception exception)
			{
				ModLog.Error("Failed to clear the current action reference: " + exception);
			}
		}
	}

	[HarmonyPatch(typeof(CharacterMainControl), nameof(CharacterMainControl.StartAction))]
	internal static class CharacterMainControl_StartAction_Patch
	{
		[HarmonyPrefix]
		private static void Prefix(CharacterMainControl __instance, CharacterActionBase newAction)
		{
			if (newAction == null)
			{
				return;
			}

			CharacterActionBase current = __instance.CurrentAction;
			if (current == null || !current.Running)
			{
				return;
			}

			if (ReferenceEquals(current, newAction))
			{
				return;
			}

			if (newAction.ActionPriority() > current.ActionPriority())
			{
				return;
			}

			if (!newAction.IsReady())
			{
				return;
			}

			ActionInterrupt.TryCancel(__instance);
		}
	}

	[HarmonyPatch(typeof(CA_UseItem), nameof(CA_UseItem.SetUseItem))]
	internal static class CA_UseItem_SetUseItem_Patch
	{
		[HarmonyPrefix]
		private static void Prefix(CA_UseItem __instance, ItemStatsSystem.Item __0)
		{
			CharacterMainControl character = __instance.characterController;
			if (!character || !__0)
			{
				return;
			}

			DuckovItemAgent heldAgent = character.CurrentHoldItemAgent;
			if (heldAgent && heldAgent.Item == __0)
			{
				return;
			}

			ActionInterrupt.TryCancel(character);
		}
	}

	[HarmonyPatch(typeof(CharacterMainControl), "Interact", new Type[0])]
	internal static class CharacterMainControl_Interact_Patch
	{
		[HarmonyPrefix]
		private static void Prefix(CharacterMainControl __instance)
		{
			CharacterActionBase current = __instance.CurrentAction;
			if (current == null)
			{
				return;
			}

			if (__instance.GetInteractableTargetToInteract() == null)
			{
				if (!current.Running)
				{
					ActionInterrupt.ClearCurrentActionRef(__instance);
				}

				return;
			}

			ActionInterrupt.TryCancel(__instance);
		}
	}

	[HarmonyPatch(typeof(ItemAgentHolder), nameof(ItemAgentHolder.SetTrigger))]
	internal static class ItemAgentHolder_SetTrigger_Patch
	{
		[HarmonyPrefix]
		private static void Prefix(ItemAgentHolder __instance, bool triggerThisFrame)
		{
			if (!triggerThisFrame)
			{
				return;
			}

			CharacterMainControl character = __instance.characterController;
			if (character)
			{
				ItemAgent_Gun gun = character.GetGun();
				if (gun && gun.BulletCount <= 0 && gun.IsReloading())
				{
					return;
				}
			}

			ActionInterrupt.TryCancel(character);
		}
	}

	[HarmonyPatch(typeof(CharacterInputControl), nameof(CharacterInputControl.OnReloadInput))]
	internal static class CharacterInputControl_OnReloadInput_Patch
	{
		[HarmonyPrefix]
		private static void Prefix(InputAction.CallbackContext context)
		{
			if (!context.performed)
			{
				return;
			}

			CharacterMainControl main = CharacterMainControl.Main;
			if (!main)
			{
				return;
			}

			if (main.CurrentAction is CA_Reload)
			{
				return;
			}

			ActionInterrupt.TryCancel(main);
		}
	}

	[HarmonyPatch(typeof(CharacterMainControl), nameof(CharacterMainControl.ChangeHoldItem))]
	internal static class CharacterMainControl_ChangeHoldItem_Patch
	{
		[HarmonyPrefix]
		private static void Prefix(CharacterMainControl __instance)
		{
			if (__instance.CanEditInventory())
			{
				return;
			}

			ActionInterrupt.TryCancel(__instance);
		}
	}
}
