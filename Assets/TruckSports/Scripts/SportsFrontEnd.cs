using UnityEngine;
using UnityEngine.InputSystem;
namespace LWS.TruckSports {
public sealed class SportsFrontEnd:SportsMenuView {
 protected override void Start(){base.Start();SportsNavigation.EnterCollection();Show(SportsNavigation.OpenSelection?1:0);SportsNavigation.OpenSelection=false;}
 public override void Action(string key){base.Action(key);switch(key){case "play":Show(1);break;case "practice":SportsNavigation.BilliardsPractice=true;SportsNavigation.LoadSport(SportsNavigation.Billiards);break;case "billiards":SportsNavigation.BilliardsPractice=false;SportsNavigation.LoadSport(SportsNavigation.Billiards);break;case "dock":SportsNavigation.LoadSport(SportsNavigation.Dock);break;case "keepaway":SportsNavigation.LoadSport(SportsNavigation.Keepaway);break;case "curling":SportsNavigation.LoadSport(SportsNavigation.Curling);break;case "basejump":SportsNavigation.LoadSport(SportsNavigation.BaseJump);break;case "cheese":SportsNavigation.LoadSport(SportsNavigation.Cheese);break;case "cornhole":SportsNavigation.LoadSport(SportsNavigation.Cornhole);break;case "wallball":SportsNavigation.LoadSport(SportsNavigation.Wallball);break;case "airhockey":SportsNavigation.LoadSport(SportsNavigation.AirHockey);break;case "bowling":SportsNavigation.LoadSport(SportsNavigation.Bowling);break;case "hockey":SportsNavigation.LoadSport(SportsNavigation.Hockey);break;case "help":Show(2);break;case "settings":SettingsText();Show(3);break;case "back":Show(0);break;case "quit":Application.Quit();break;}}
 protected override void Update(){base.Update();bool back=SportsDeviceInput.KeyDown(Key.Escape);foreach(var p in Gamepad.all)back|=menuInput.Down(p,8);if(back&&Page!=0)Show(0);}
}
}
