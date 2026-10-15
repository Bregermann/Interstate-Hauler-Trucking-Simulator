namespace LWS.TruckSports.Editor {public static class SportsExpansionStageAuthoring {
 public static void PrepareFinal(){SportsExpansionAuthoring.SceneWeather();LWS.TruckBaseJumping.Editor.BaseJumpAuthoring.Create();SportsExpansionFront.Upgrade();SportsExpansionFront.Validate();}
 public static void BaseViewBuild(){SportsExpansionAuthoring.SceneWeather();LWS.TruckBaseJumping.Editor.BaseJumpAuthoring.Create();SportsExpansionFront.Upgrade();SportsBuilder.Build();}
 public static void StageBuild(){SportsExpansionAuthoring.ExistingSports();LWS.TruckKeepaway.Editor.KeepawayAuthoring.Create();LWS.TruckDockDiving.Editor.DockAuthoring.Create();LWS.TruckCurling.Editor.CurlingAuthoring.Create();LWS.TruckBaseJumping.Editor.BaseJumpAuthoring.Create();SportsExpansionFront.Upgrade();SportsBuilder.Build();}
}}
