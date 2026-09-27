using System.Linq;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests.EditMode
{
    [Category("TaxiPolish")]
    public sealed class TruckTaxiPolishDialogueAuthoringTests
    {
        [Test]
        public void CuratedLinesAreAdditiveIdentitySpecificAndIdempotent()
        {
            var smuggler = ScriptableObject.CreateInstance<PassengerProfile>();
            var scientist = ScriptableObject.CreateInstance<PassengerProfile>();
            var smugglerDialogue = ScriptableObject.CreateInstance<TruckTaxiDialogueSet>();
            var scientistDialogue = ScriptableObject.CreateInstance<TruckTaxiDialogueSet>();
            try
            {
                smuggler.passengerId = "smuggler";
                smuggler.authoredDialogue = smugglerDialogue;
                scientist.passengerId = "time-scientist";
                scientist.authoredDialogue = scientistDialogue;
                var original = new TruckTaxiDialogueLine {
                    lineId = "original", passengerId = "smuggler", category = TruckTaxiDialogueCategory.PickupGreeting,
                    text = "Preserve this authored line."
                };
                smugglerDialogue.lines = new[] { original };

                Assert.AreEqual(6, TruckTaxiPolishDialogueAuthoring.ApplyCuratedCoverage(new[] { smuggler, scientist }));
                Assert.AreSame(original, smugglerDialogue.lines[0]);
                Assert.AreEqual("Preserve this authored line.", smugglerDialogue.lines[0].text);
                Assert.AreEqual(0, TruckTaxiPolishDialogueAuthoring.ApplyCuratedCoverage(new[] { smuggler, scientist }));
                Assert.AreEqual(5, smugglerDialogue.lines.Length);
                Assert.AreEqual(2, scientistDialogue.lines.Length);
                Assert.That(smugglerDialogue.lines.Single(line => line.lineId == "regional-polish-sketchyexit").text, Does.Contain("unspecified exchange"));
                Assert.That(scientistDialogue.lines.Single(line => line.lineId == "regional-polish-stoppedtoolong").text, Does.Contain("elapsed time"));
                Assert.That(smugglerDialogue.lines.Concat(scientistDialogue.lines)
                    .Where(line => line.lineId.StartsWith("regional-polish-"))
                    .All(line => line.generatedAudio == null && line.voiceProfile == null), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(smugglerDialogue);
                Object.DestroyImmediate(scientistDialogue);
                Object.DestroyImmediate(smuggler);
                Object.DestroyImmediate(scientist);
            }
        }
    }
}
