using NUnit.Framework;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace LastSeenWearing.Tests.Project
{
    /// <summary>
    /// P0.07: the localization setup docs/LOCALIZATION.md §6 describes is in place, and the keys
    /// it lists have an English value.
    /// </summary>
    public sealed class LocalizationTests
    {
        private static readonly LocaleIdentifier English = new LocaleIdentifier("en");

        [Test]
        public void LocalizationSettingsAreActive()
        {
            Assert.That(LocalizationEditorSettings.ActiveLocalizationSettings, Is.Not.Null);
        }

        [Test]
        public void EnglishIsAProjectLocale()
        {
            Assert.That(LocalizationEditorSettings.GetLocale(English), Is.Not.Null, "no 'en' locale");
        }

        [Test]
        public void EnglishIsTheStartupLocale()
        {
            var selectors = LocalizationEditorSettings.ActiveLocalizationSettings.GetStartupLocaleSelectors();
            Assert.That(selectors, Is.Not.Empty, "no startup locale selector");
            Assert.That(selectors[0], Is.InstanceOf<SpecificLocaleSelector>());
            Assert.That(((SpecificLocaleSelector)selectors[0]).LocaleId, Is.EqualTo(English));
        }

        [TestCase("UI")]
        [TestCase("Roles")]
        [TestCase("Festival")]
        public void TheTableExistsInEnglish(string tableName)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            Assert.That(collection, Is.Not.Null, $"no '{tableName}' string table collection");
            Assert.That(collection.GetTable(English), Is.Not.Null, $"'{tableName}' has no English table");
        }

        [TestCase("UI", "ui.menu.title")]
        [TestCase("Roles", "role.watcher.name")]
        [TestCase("Roles", "role.patrol.name")]
        [TestCase("Roles", "role.plainclothes.name")]
        [TestCase("Roles", "role.dog.name")]
        [TestCase("Roles", "role.fugitive.name")]
        public void TheKeyHasAnEnglishValue(string tableName, string key)
        {
            var table = (StringTable)LocalizationEditorSettings.GetStringTableCollection(tableName).GetTable(English);
            var entry = table.GetEntry(key);
            Assert.That(entry, Is.Not.Null, $"'{key}' is missing from {tableName}_en");
            Assert.That(entry.Value, Is.Not.Empty, $"'{key}' has no English value");
        }
    }
}
