namespace LWS.TruckPedestrianPlowing {
 public sealed class PlowCabMatch:PlowMatch {
  public override string Title=>"PEDESTRIAN PLOWING · CAB CHAOS";
  public override string OptionLabel=>"DRIVER CAB · GPS + PISS JUG";
  public override void NormalizeSetup(){base.NormalizeSetup();option=2;cabExperience=true;}
  public override void ExtraAction(string key){if(key=="cabinfo")menus.Show(LWS.TruckSports.SportsEventScreen.Help);else base.ExtraAction(key);}
  public override string Help=>base.Help+"\n\nCAB CHAOS\nA separate first-person version with Taxi's full cab, animated steering, free look, Compass GPS routing to reachable positive targets and an independent piss jug for each driver. Hold the displayed jug binding and tap the cue binding at each of three timing markers. Driving/turning too sharply can spill it. This optional distraction never injects points or changes target scoring. Pause/reconnect preserves pressure, progress and inventory. Keyboard jug bindings follow your selected keyboard cluster; whole pads use LB/RB, shared left uses LT/left-stick click, shared right uses RT/right-stick click.";
 }
}
