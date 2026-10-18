using System;
namespace LWS.TruckPoker {
// Cards are 0..51: rank 2..A within each suit. Larger packed ranks always win.
public static class PokerHand {
 public static int Rank(int card)=>card%13+2;
 public static int Suit(int card)=>card/13;
 public static string Name(int card){if(card<0||card>=52)return "BACK";return (card%13==8?"10":"23456789TJQKA"[card%13].ToString())+new[]{"♣","♦","♥","♠"}[card/13];}
 public static string Category(long value)=>new[]{"HIGH CARD","PAIR","TWO PAIR","THREE OF A KIND","STRAIGHT","FLUSH","FULL HOUSE","FOUR OF A KIND","STRAIGHT FLUSH"}[(int)(value>>20)];
 static long Pack(int category,int a=0,int b=0,int c=0,int d=0,int e=0)=>(long)category<<20|(long)a<<16|(long)b<<12|(long)c<<8|(long)d<<4|(long)e;
 public static long Evaluate(params int[] cards){if(cards==null||cards.Length<5||cards.Length>7)throw new ArgumentException("Five to seven cards required");ulong used=0;foreach(int c in cards){if(c<0||c>=52||(used&(1UL<<c))!=0)throw new ArgumentException("Invalid/duplicate card");used|=1UL<<c;}long best=-1;for(int a=0;a<cards.Length-4;a++)for(int b=a+1;b<cards.Length-3;b++)for(int c=b+1;c<cards.Length-2;c++)for(int d=c+1;d<cards.Length-1;d++)for(int e=d+1;e<cards.Length;e++)best=Math.Max(best,Five(cards[a],cards[b],cards[c],cards[d],cards[e]));return best;}
 static long Five(int a,int b,int c,int d,int e){int[] ranks={Rank(a),Rank(b),Rank(c),Rank(d),Rank(e)};Array.Sort(ranks);Array.Reverse(ranks);bool flush=Suit(a)==Suit(b)&&Suit(a)==Suit(c)&&Suit(a)==Suit(d)&&Suit(a)==Suit(e);int straight=0;if(ranks[0]-ranks[4]==4&&ranks[0]>ranks[1]&&ranks[1]>ranks[2]&&ranks[2]>ranks[3]&&ranks[3]>ranks[4])straight=ranks[0];else if(ranks[0]==14&&ranks[1]==5&&ranks[2]==4&&ranks[3]==3&&ranks[4]==2)straight=5;if(flush&&straight>0)return Pack(8,straight);
  int four=0,three=0,pair=0,pair2=0;int[] kick=new int[5];int n=0;for(int i=0;i<5;){int count=1;while(i+count<5&&ranks[i+count]==ranks[i])count++;if(count==4)four=ranks[i];else if(count==3)three=ranks[i];else if(count==2){if(pair==0)pair=ranks[i];else pair2=ranks[i];}else kick[n++]=ranks[i];i+=count;}
  if(four>0)return Pack(7,four,kick[0]);if(three>0&&pair>0)return Pack(6,three,pair);if(flush)return Pack(5,ranks[0],ranks[1],ranks[2],ranks[3],ranks[4]);if(straight>0)return Pack(4,straight);if(three>0)return Pack(3,three,kick[0],kick[1]);if(pair2>0)return Pack(2,pair,pair2,kick[0]);if(pair>0)return Pack(1,pair,kick[0],kick[1],kick[2]);return Pack(0,ranks[0],ranks[1],ranks[2],ranks[3],ranks[4]);
 }
}
}
