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

			CharacterActionBase current = character.CurrentAction;
			if (!current || !current.Running)
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
			if (current == null || !current.Running || current is CA_Carry || !current.IsStopable())
			{
				return;
			}

			if (ReferenceEquals(current, newAction))
			{
				if (newAction.IsReady())
				{
					ActionInterrupt.TryCancel(__instance);
				}

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

			if (__instance.carryAction != null && __instance.carryAction.Running)
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

			if (!InputManager.InputActived)
			{
				return;
			}

			ActionInterrupt.TryCancel(__instance.characterController);
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
