namespace LWS.TruckCheeseRolling {
// A fresh accelerator edge during displayed 2, continuously held until GO.
public struct CheeseStartBoost {
 public bool Held{get;private set;}public bool Armed{get;private set;}
 public void Sample(float elapsed,bool held){if(!held)Armed=false;else if(!Held)Armed=elapsed>=1&&elapsed<2;Held=held;}
}
}
