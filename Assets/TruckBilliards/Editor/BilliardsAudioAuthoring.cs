using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace LWS.TruckBilliards.Editor
{
 public static class BilliardsAudioAuthoring
 {
  // Original synthesized resonators/noise and short tonal stings; no downloaded samples.
  public static AudioClip[] Generate(){
   string root=BilliardsBuilder.Root+"/Audio";Directory.CreateDirectory(root);var clips=new AudioClip[8];
   for(int kind=0;kind<8;kind++){string path=root+"/"+((BilliardsSound)kind)+".wav";
    if(!File.Exists(path)){int rate=44100;float duration=kind==7?.8f:kind==6?.5f:kind==5?.24f:kind==4?.14f:kind==2?.2f:kind==1?.09f:.07f;int count=(int)(rate*duration);uint random=12345;
     using(var writer=new BinaryWriter(File.Create(path))){writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
      for(int i=0;i<count;i++){float t=(float)i/rate;random^=random<<13;random^=random>>17;random^=random<<5;double noise=(random%10000)/5000.0-1;double value;
       if(kind<4){double hz=kind==0?1850:kind==1?185:kind==2?370:610;double decay=kind==0?95:kind==1?55:kind==2?22:85;value=(Math.Sin(2*Math.PI*hz*t)+.32*Math.Sin(2*Math.PI*hz*1.63*t)+noise*(kind==1?.55:.2))*Math.Exp(-decay*t);}
       else{double hz=kind==4?660:kind==5?330:kind==6?523.25:659.25;int note=(int)(t/(kind==7?.16:.12));double ratio=kind==5?Math.Pow(.89,note):Math.Pow(1.259921,note%4);float local=t% (kind==7?.16f:.12f);value=Math.Sin(2*Math.PI*hz*ratio*t)*Math.Exp(-local*17)*Math.Min(1,local*400);}
       double end=Math.Min(1,(duration-t)*100);writer.Write((short)(Math.Clamp(value*.48*end,-.95,.95)*short.MaxValue));
      }
     }
    }
    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();clips[kind]=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
   }return clips;
  }
 }
}
