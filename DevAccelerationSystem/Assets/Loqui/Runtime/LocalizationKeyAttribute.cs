using UnityEngine;

namespace Loqui
{
    /// <summary>
    /// Marks a string field as a localization key. In the editor the field renders as a searchable
    /// dropdown of the keys available across the project's <see cref="LocalizationCatalog"/> assets,
    /// instead of a free text field. Free text entry stays available for not-yet-authored keys.
    /// </summary>
    public sealed class LocalizationKeyAttribute : PropertyAttribute
    {
    }
}
