using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PiGame.Input
{
    public class InputBindingService
    {
        private const string PlayerPrefsKey = "PiGame.InputBindings.v2";
        private const string LegacyPlayerPrefsKey = "PiGame.InputBindings.v1";

        private readonly InputActionAsset _actions;

        public InputBindingService(InputActionAsset actions)
        {
            _actions = actions != null
                ? actions
                : throw new ArgumentNullException(nameof(actions));
        }

        public bool HasSavedBindings => PlayerPrefs.HasKey(PlayerPrefsKey);

        public void Load()
        {
            if (PlayerPrefs.HasKey(LegacyPlayerPrefsKey))
            {
                PlayerPrefs.DeleteKey(LegacyPlayerPrefsKey);
                PlayerPrefs.Save();
            }

            if (!HasSavedBindings)
            {
                return;
            }

            string overridesJson = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(overridesJson))
            {
                return;
            }

            try
            {
                _actions.LoadBindingOverridesFromJson(overridesJson);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Não foi possível carregar os controles salvos. "
                    + $"Os padrões serão utilizados. {exception.Message}");
                ResetAll();
            }
        }

        public void Save()
        {
            string overridesJson = _actions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(PlayerPrefsKey, overridesJson);
            PlayerPrefs.Save();
        }

        public void ResetAll()
        {
            _actions.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }

        public void ResetBindingGroup(string bindingGroup)
        {
            if (string.IsNullOrWhiteSpace(bindingGroup))
            {
                throw new ArgumentException(
                    "O grupo de bindings deve ser informado.",
                    nameof(bindingGroup));
            }

            foreach (InputActionMap actionMap in _actions.actionMaps)
            {
                foreach (InputAction action in actionMap.actions)
                {
                    for (int bindingIndex = 0;
                         bindingIndex < action.bindings.Count;
                         bindingIndex++)
                    {
                        InputBinding binding = action.bindings[bindingIndex];
                        if (BelongsToGroup(binding.groups, bindingGroup))
                        {
                            action.RemoveBindingOverride(bindingIndex);
                        }
                    }
                }
            }

            Save();
        }

        private static bool BelongsToGroup(string groups, string expectedGroup)
        {
            if (string.IsNullOrWhiteSpace(groups))
            {
                return false;
            }

            string[] bindingGroups = groups.Split(';');
            foreach (string bindingGroup in bindingGroups)
            {
                if (string.Equals(
                        bindingGroup,
                        expectedGroup,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
