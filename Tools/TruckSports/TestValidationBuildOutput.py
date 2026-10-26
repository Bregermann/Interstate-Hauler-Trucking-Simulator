"""Read-only regression checks for candidate output and project junction safety."""
import unittest
from RunBroadcastValidation import validate_candidate_build_arguments, ROOT, CLEAN


class ValidationBuildOutputTests(unittest.TestCase):
    def test_missing_output_never_reaches_unity(self):
        with self.assertRaises(ValueError):
            validate_candidate_build_arguments(["-quit", "-executeMethod", "BuildFocused"])

    def test_default_canonical_path_is_rejected(self):
        with self.assertRaises(ValueError):
            validate_candidate_build_arguments(["-tsBuildOutput", "Builds/TruckSports/TruckSports.exe"])

    def test_absolute_canonical_alias_is_rejected(self):
        with self.assertRaises(ValueError):
            validate_candidate_build_arguments(["-tsBuildOutput", str(CLEAN / "Builds/TruckSports/TruckSports.exe")])

    def test_duplicate_output_flags_are_rejected(self):
        with self.assertRaises(ValueError):
            validate_candidate_build_arguments(["-tsBuildOutput", "Builds/TruckSportsCamera/TruckSports.exe", "-tsBuildOutput", "Builds/TruckSports/TruckSports.exe"])

    def test_actual_relative_candidate_resolves_through_junction(self):
        self.assertEqual(validate_candidate_build_arguments(["-tsBuildOutput", "Builds/TruckSportsCamera/TruckSports.exe"]), (ROOT / "Builds/TruckSportsCamera/TruckSports.exe").resolve())

    def test_absolute_candidate_is_allowed(self):
        self.assertEqual(validate_candidate_build_arguments(["-tsBuildOutput", str(ROOT / "Builds/TruckSportsCamera/TruckSports.exe")]), (ROOT / "Builds/TruckSportsCamera/TruckSports.exe").resolve())


if __name__ == "__main__":
    unittest.main()
