#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Pure synthetic validation inputs only. These tests never import/capture art,
// call an installer, rewrite a receipt or claim an actual Windows pass.
public sealed class ImpossiblePreviewImportTests
{
    [Serializable] sealed class Capture { public string key, scene, sourceFile, sha256; }
    [Serializable] sealed class Review
    {
        public int schema = 1;
        public bool reviewed = true;
        public string reviewId = "PURE FIXTURE - NOT AN ACTUAL WINDOWS REVIEW", receiptSha256 = new string('a', 64), progressSha256 = new string('b', 64);
        public Capture[] captures;
    }
    static Type Author => Type.GetType("LWS.TruckSports.Editor.SportsImpossiblePreviewAuthoring, Assembly-CSharp-Editor", true);
    static object Invoke(string method, params object[] args) => Author.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
    static object[] Entries => ((IEnumerable)Type.GetType("LWS.TruckSports.SportsImpossibleGames, Assembly-CSharp", true).GetProperty("All").GetValue(null)).Cast<object>().ToArray();
    static string Field(object owner, string name) => (string)owner.GetType().GetField(name).GetValue(owner);
    static Review ValidReview() => new Review { captures = Entries.Select((entry, index) => new Capture { key = Field(entry, "key"), scene = Field(entry, "scene"),
        sourceFile = Field(entry, "scene") + "-natural-gameplay-1280.png", sha256 = index.ToString("x64") }).ToArray() };
    static void Reject(string method, params object[] args)
    {
        var error = Assert.Throws<TargetInvocationException>(() => Invoke(method, args));
        Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
    }
    static string Progress()
    {
        return string.Join("\n", Entries.SelectMany(entry => new[] { "existing navigation opens correct saved authority", "ordinary AI setup starts the actual match",
            "actual match remains in a valid gameplay/results state", "actual active camera renders into the player backbuffer",
            "natural-gameplay has actual 1280x720 backbuffer and PNG dimensions" }.Select(message => "PASS " + Field(entry, "scene") + " " + message))) + "\n";
    }
    static string Receipt(string progress) => progress + "CHECKS=" + progress.Split('\n').Count(line => line.StartsWith("PASS ", StringComparison.Ordinal)) +
        " ERRORS=0 SCENES=21 SCREENSHOTS=86 INTERRUPTED=False\nActual Windows composed frames, ordinary saved setup/AI opening, pause/resume/rematch/exit only.\n";

    [Test]
    public void PureCompletedTwentyOneOpeningReceiptAndReviewedDistinctMappingsPassWithoutImporting()
    {
        string progress = Progress();
        Assert.DoesNotThrow(() => Invoke("ValidateReceipt", Receipt(progress), progress));
        Assert.DoesNotThrow(() => Invoke("ValidateReviewedMappings", JsonUtility.ToJson(ValidReview())));
    }

    [Test]
    public void PendingFailedInterruptedMissingDuplicateAndMiscountedReceiptsAreRefused()
    {
        string progress = Progress(), receipt = Receipt(progress);
        foreach (string changed in new[] { "RUNNING 2026-10-26", receipt + "FAIL TruckBakeOff real completion\n", receipt + "ERROR TruckTeaParty exception\n",
            receipt.Replace("ERRORS=0", "ERRORS=1"), receipt.Replace("INTERRUPTED=False", "INTERRUPTED=True"), receipt.Replace("SCENES=21", "SCENES=20"),
            receipt.Replace("CHECKS=105", "CHECKS=104"), receipt.Replace("SCREENSHOTS=86", "SCREENSHOTS=84"), receipt + "CHECKS=105 ERRORS=0 SCENES=21 SCREENSHOTS=86 INTERRUPTED=False\n" })
            Reject("ValidateReceipt", changed, progress);
        Reject("ValidateReceipt", receipt, progress + "ERROR TruckBakeOff late callback\n");
    }

    [Test]
    public void EveryActualNavigationStartBackbufferAndNaturalCapturePassIsRequiredInBothHashedFiles()
    {
        string progress = Progress(), receipt = Receipt(progress);
        string first = progress.Split('\n')[0] + "\n";
        Reject("ValidateReceipt", receipt, progress.Replace(first, ""));
        Reject("ValidateReceipt", receipt.Replace(first, ""), progress);
        Reject("ValidateReceipt", receipt, progress + first);
        string natural = "PASS TruckBakeOff natural-gameplay has actual 1280x720 backbuffer and PNG dimensions";
        Reject("ValidateReceipt", receipt.Replace(natural, natural.Replace("1280x720", "640x480")), progress);
    }

    [Test]
    public void UnreviewedUnknownMissingDuplicateOrReusedImageMappingsCannotBecomeNewCards()
    {
        var review = ValidReview(); review.reviewed = false; Reject("ValidateReviewedMappings", JsonUtility.ToJson(review));
        review = ValidReview(); review.reviewId = " "; Reject("ValidateReviewedMappings", JsonUtility.ToJson(review));
        review = ValidReview(); review.receiptSha256 = "RUNNING"; Reject("ValidateReviewedMappings", JsonUtility.ToJson(review));
        review = ValidReview(); review.captures = review.captures.Take(20).ToArray(); Reject("ValidateReviewedMappings", JsonUtility.ToJson(review));
        review = ValidReview(); review.captures[1].scene = review.captures[0].scene; Reject("ValidateReviewedMappings", JsonUtility.ToJson(review));
        review = ValidReview(); review.captures[1].sha256 = review.captures[0].sha256; Reject("ValidateReviewedMappings", JsonUtility.ToJson(review));
        review = ValidReview(); review.captures[0].key = "legacy"; Reject("ValidateReviewedMappings", JsonUtility.ToJson(review));
        review = ValidReview(); review.captures[0].scene = "TruckPlowing"; Reject("ValidateReviewedMappings", JsonUtility.ToJson(review));
    }

    [TestCase("../TruckTruckGoose-natural-gameplay-1280.png")]
    [TestCase("TruckTruckGoose-setup-1280.png")]
    [TestCase("TruckTruckGoose-natural-gameplay-1920.png")]
    [TestCase("C:/old/game.png")]
    public void OnlyExactFlatNewSportNatural720CaptureFilenameIsAccepted(string name)
    {
        var review = ValidReview(); review.captures[0].sourceFile = name;
        Reject("ValidateReviewedMappings", JsonUtility.ToJson(review));
    }

    static byte[] Header()
    {
        var bytes = new byte[33]; byte[] signature = {137,80,78,71,13,10,26,10}; Array.Copy(signature, bytes, 8);
        bytes[11]=13; bytes[12]=(byte)'I'; bytes[13]=(byte)'H'; bytes[14]=(byte)'D'; bytes[15]=(byte)'R';
        bytes[18]=5; bytes[22]=2; bytes[23]=208; bytes[24]=8; bytes[25]=6; return bytes;
    }
    [Test]
    public void ExactPngHeaderRejectsTruncationWrongDimensionsEncodingAndSignature()
    {
        Assert.That((bool)Invoke("HasExactPreviewHeader", Header()), Is.True);
        var rgb = Header(); rgb[25]=2; Assert.That((bool)Invoke("HasExactPreviewHeader", rgb), Is.True);
        Assert.That((bool)Invoke("HasExactPreviewHeader", (object)null), Is.False);
        Assert.That((bool)Invoke("HasExactPreviewHeader", new byte[24]), Is.False);
        foreach (int index in new[] {0,11,12,18,19,22,23,24,25,26,27,28})
        {
            var bytes = Header(); bytes[index]++; Assert.That((bool)Invoke("HasExactPreviewHeader", bytes), Is.False, "byte" + index);
        }
    }
}
#endif
