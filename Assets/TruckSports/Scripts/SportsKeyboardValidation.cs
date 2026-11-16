using System;
using TMPro;
using UnityEngine;
namespace LWS.TruckSports
{
    // Use TMP's own validation/limit semantics on a detached, listener-free field.
    public sealed class SportsKeyboardValidation : TMP_InputField
    {
        [Serializable] sealed class RegexSettings { public string m_RegexValue; }
        public void Configure(TMP_InputField source)
        {
            contentType = source.contentType;
            inputValidator = source.inputValidator;
            characterValidation = source.characterValidation;
            onValidateInput = source.onValidateInput;
            characterLimit = source.characterLimit;
            lineType = source.lineType;
            var regex = JsonUtility.FromJson<RegexSettings>(JsonUtility.ToJson(source));
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(regex), this);
        }
        public string AppendValidated(string original, string addition)
        {
            SetTextWithoutNotify(original);
            // The public caret setter requires a rendered TMP mesh. This field
            // is deliberately detached and inactive: use TMP's protected edit
            // positions and keep its actual Append/Validate implementation.
            m_StringPosition = m_StringSelectPosition = original.Length;
            m_CaretPosition = m_CaretSelectPosition = 0;
            Append(addition);
            return text;
        }
    }
}
