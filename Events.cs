using Microsoft.Xna.Framework;
using StardewValley;
using StardewValleyMP.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP
{
    class Events
    {
        public static void fix()
        {
            Event @event = Game1.currentLocation.currentEvent;
            Dictionary<string, string> data = (Dictionary<string, string>)Util.GetInstanceField(typeof(Event), @event, "festivalData");

            fixCommands( @event );
            if ( @event.FestivalName.Equals("Flower Dance"))
            {
                fixFlowerDance( @event, data );
            }
        }

        public static void reset()
        {
            didFixDance = false;
            prevCommandCount = prevCommand = -1;
        }

        public static int prevCommandCount = -1, prevCommand = -1;
        private static void fixCommands( Event @event )
        {
            if ( @event.isFestival && prevCommandCount != @event.eventCommands.Count() && prevCommandCount != -1 )
            {
                // I'll worry about other festivals another time
                if (@event.FestivalName.Equals("Flower Dance"))
                    Multiplayer.sendFunc(new ContinueEventPacket());
            }
            prevCommandCount = @event.eventCommands.Count();

            if (prevCommand == @event.currentCommand)
                return;

            for (int i = prevCommand + 1; i <= @event.currentCommand; ++i)
            {
                string[] array = @event.eventCommands[Math.Min(@event.eventCommands.Count<string>() - 1, i)].Split(new char[]
			    {
				    ' '
			    });
                fixCommand(@event, array);
            }
            prevCommand = @event.currentCommand;
        }
        private static void fixCommand( Event @event, string[] array )
        {
            // We only want to do things the game would have done to farmers
            // So stuff affecting the NPCs or anything else was removed

            // More stuff from Event
            Dictionary<string, Vector3> actorPositionsAfterMove = (Dictionary<string, Vector3>)Util.GetInstanceField(typeof(Event), @event, "actorPositionsAfterMove");
            
            if (array[0].Equals("showFrame"))
            {
                if (array.Count<string>() > 2 && !array[2].Equals("flip") && !array[1].Contains("farmer"))
                {
                }
                else
                {
                    SFarmer farmer = getFarmerFromFarmerNumberString(array[1]);
                    //if (farmer == Game1.player) return;
                    if (array.Count<string>() == 2)
                    {
                        farmer = Game1.player;
                    }
                    if (farmer != null)
                    {
                        if (array.Count<string>() > 2)
                        {
                            array[1] = array[2];
                        }
                        List<FarmerSprite.AnimationFrame> list = new List<FarmerSprite.AnimationFrame>();
                        list.Add(new FarmerSprite.AnimationFrame(Convert.ToInt32(array[1]), 100, false, array.Count<string>() > 2, null, false));
                        farmer.FarmerSprite.setCurrentAnimation(list.ToArray());
                        farmer.FarmerSprite.loopThisAnimation = true;
                        farmer.FarmerSprite.PauseForSingleAnimation = true;
                        farmer.sprite.CurrentFrame = Convert.ToInt32(array[1]);
                    }
                }
            }
            else if (array[0].Equals("animate"))
            {
                int milliseconds = Convert.ToInt32(array[4]);
                bool flip = array[2].Equals("true");
                bool flag2 = array[3].Equals("true");
                List<FarmerSprite.AnimationFrame> list2 = new List<FarmerSprite.AnimationFrame>();
                for (int n = 5; n < array.Count<string>(); n++)
                {
                    list2.Add(new FarmerSprite.AnimationFrame(Convert.ToInt32(array[n]), milliseconds, false, flip, null, false));
                }
                if (array[1].Contains("farmer"))
                {
                    SFarmer farmerFromFarmerNumberString6 = getFarmerFromFarmerNumberString(array[1]);
                    //if (farmerFromFarmerNumberString6 == Game1.player) return;
                    if (farmerFromFarmerNumberString6 != null)
                    {
                        farmerFromFarmerNumberString6.FarmerSprite.setCurrentAnimation(list2.ToArray());
                        farmerFromFarmerNumberString6.FarmerSprite.loopThisAnimation = flag2;
                        farmerFromFarmerNumberString6.FarmerSprite.PauseForSingleAnimation = true;
                    }
                }
                else
                {
                }
            }
            else if (array[0].Equals("stopAnimation"))
            {
                if (array[1].Contains("farmer"))
                {
                    SFarmer farmerFromFarmerNumberString7 = getFarmerFromFarmerNumberString(array[1]);
                    //if (farmerFromFarmerNumberString7 == Game1.player) return;
                    if (farmerFromFarmerNumberString7 != null)
                    {
                        farmerFromFarmerNumberString7.completelyStopAnimatingOrDoingAction();
                        farmerFromFarmerNumberString7.Halt();
                        farmerFromFarmerNumberString7.FarmerSprite.currentAnimation = null;
                        int num12 = farmerFromFarmerNumberString7.facingDirection;
                        switch (num12)
                        {
                            case 0:
                                farmerFromFarmerNumberString7.FarmerSprite.setCurrentSingleFrame(12, 32000, false, false);
                                break;
                            case 1:
                                farmerFromFarmerNumberString7.FarmerSprite.setCurrentSingleFrame(6, 32000, false, false);
                                break;
                            case 2:
                                farmerFromFarmerNumberString7.FarmerSprite.setCurrentSingleFrame(0, 32000, false, false);
                                break;
                            case 3:
                                farmerFromFarmerNumberString7.FarmerSprite.setCurrentSingleFrame(6, 32000, false, true);
                                break;
                        }
                    }
                }
                else
                {
                }
            }
            else if (array[0].Equals("positionOffset"))
            {
                if (array[1].Contains("farmer"))
                {
                    SFarmer farmerFromFarmerNumberString8 = getFarmerFromFarmerNumberString(array[1]);
                    //if (farmerFromFarmerNumberString8 == Game1.player) return;
                    if (farmerFromFarmerNumberString8 != null)
                    {
                        SFarmer expr_46E6_cp_0 = farmerFromFarmerNumberString8;
                        expr_46E6_cp_0.position.X = expr_46E6_cp_0.position.X + (float)Convert.ToInt32(array[2]);
                        SFarmer expr_4702_cp_0 = farmerFromFarmerNumberString8;
                        expr_4702_cp_0.position.Y = expr_4702_cp_0.position.Y + (float)Convert.ToInt32(array[3]);
                    }
                }
                else
                {
                }
            }
            else if (array[0].Equals("move"))
            {
                int num3 = 1;
                while (num3 < array.Count<string>() && array.Count<string>() - num3 >= 3)
                {
                    if (array[num3].Contains("farmer") && !actorPositionsAfterMove.ContainsKey(array[num3]))
                    {
                        SFarmer farmerFromFarmerNumberString2 = Utility.getFarmerFromFarmerNumberString(array[num3]);
                        //if (farmerFromFarmerNumberString2 == Game1.player) { num3 += 4; continue; }
                        if (farmerFromFarmerNumberString2 != null)
                        {
                            farmerFromFarmerNumberString2.canOnlyWalk = false;
                            farmerFromFarmerNumberString2.setRunning(false, true);
                            farmerFromFarmerNumberString2.canOnlyWalk = true;
                            farmerFromFarmerNumberString2.convertEventMotionCommandToMovement(new Vector2((float)Convert.ToInt32(array[num3 + 1]), (float)Convert.ToInt32(array[num3 + 2])));
                            actorPositionsAfterMove.Add(array[num3], @event.getPositionAfterMove(Game1.player, Convert.ToInt32(array[num3 + 1]), Convert.ToInt32(array[num3 + 2]), Convert.ToInt32(array[num3 + 3])));
                        }
                    }
                    else
                    {
                    }
                    num3 += 4;
                }
                if (array.Last<string>().Equals("true"))
                {
                }
                if (!array.Last<string>().Equals("false"))
                {
                }
                if (array.Count<string>() == 2 && actorPositionsAfterMove.Count == 0)
                {
                }
            }
            else if (array[0].Equals("emote"))
            {
                bool flag = array.Count<string>() > 3;
                if (array[1].Contains("farmer"))
                {
                    SFarmer farmerFromFarmerNumberString3 = getFarmerFromFarmerNumberString(array[1]);
                    //if (farmerFromFarmerNumberString3 == Game1.player) return;
                    if (farmerFromFarmerNumberString3 != null)
                    {
                        Game1.player.doEmote(Convert.ToInt32(array[2]), !flag);
                    }
                }
                else
                {
                }
            }
            else if (array[0].Equals("faceDirection"))
            {
                if (array[1].Contains("farmer"))
                {
                    SFarmer farmerFromFarmerNumberString4 = getFarmerFromFarmerNumberString(array[1]);
                    //if (farmerFromFarmerNumberString4 == Game1.player) return;
                    if (farmerFromFarmerNumberString4 != null)
                    {
                        farmerFromFarmerNumberString4.FarmerSprite.StopAnimation();
                        farmerFromFarmerNumberString4.completelyStopAnimatingOrDoingAction();
                        farmerFromFarmerNumberString4.faceDirection(Convert.ToInt32(array[2]));
                        farmerFromFarmerNumberString4.FarmerSprite.StopAnimation();
                    }
                }
                else if (array[1].Contains("spouse"))
                {
                }
                else
                {
                }
                if (array.Length == 3 && Game1.pauseTime <= 0f)
                {
                }
                if (array.Length > 3)
                {
                }
            }
            else if (array[0].Equals("warp"))
            {
                if (array[1].Contains("farmer"))
                {
                    SFarmer farmerFromFarmerNumberString5 = getFarmerFromFarmerNumberString(array[1]);
                    //if (farmerFromFarmerNumberString5 == Game1.player) return;
                    if (farmerFromFarmerNumberString5 != null)
                    {
                        farmerFromFarmerNumberString5.position.X = (float)(Convert.ToInt32(array[2]) * Game1.tileSize);
                        farmerFromFarmerNumberString5.position.Y = (float)(Convert.ToInt32(array[3]) * Game1.tileSize);
                        if (Game1.IsClient)
                        {
                            farmerFromFarmerNumberString5.remotePosition = new Vector2(farmerFromFarmerNumberString5.position.X, farmerFromFarmerNumberString5.position.Y);
                        }
                    }
                }
                else if (array[1].Contains("spouse"))
                {
                }
                else
                {
                }
                if (array.Count<string>() > 4)
                {
                }
            }
            if (array[0].Equals("proceedPosition"))
            {/*
                try
                {
                    if (!this.getCharacterByName(array[1]).isMoving() || (this.npcControllers != null && this.npcControllers.Count<NPCController>() == 0))
                    {
                        this.getCharacterByName(array[1]).Halt();
                        this.CurrentCommand++;
                    }
                    goto IL_4406;
                }*/
                // TODO: Fix this^ later
            }
        }
        private static SFarmer getFarmerFromFarmerNumberString(string s)
        {
            if (s.Equals("farmer"))
            {
                return Multiplayer.getFarmer(0);
            }
            return Multiplayer.getFarmer((byte)(Convert.ToInt32(string.Concat(s[s.Count<char>() - 1])) - 1));
        }

        private static bool didFixDance = false;
        private static void fixFlowerDance( Event dance, Dictionary<string, string> data )
        {
            if (dance.playerControlSequence || didFixDance)
                return;
            if (dance.eventCommands[0] != "pause 500")
                return;
            Log.trace("Flower dance beginning, fixing");
            didFixDance = true;

            // Copied from Event.setupFestivalMainEvent
            // All I did was make it use our players
            List<string> list = new List<string>();
            List<string> list2 = new List<string>();
            List<string> list3 = new List<string>
			{
				"Abigail",
				"Penny",
				"Leah",
				"Maru",
				"Haley"
			};
            List<string> list4 = new List<string>
			{
				"Sebastian",
				"Sam",
				"Elliott",
				"Harvey",
				"Alex"
			};
            for (int i = 0; i < Multiplayer.getFarmerCount(); i++)
            {
                SFarmer farmerFromFarmerNumber = Multiplayer.getFarmer( ( byte ) i );
                if (farmerFromFarmerNumber.dancePartner != null)
                {
                    if (farmerFromFarmerNumber.dancePartner.gender == 1)
                    {
                        list.Add(farmerFromFarmerNumber.dancePartner.name);
                        list3.Remove(farmerFromFarmerNumber.dancePartner.name);
                        list2.Add("farmer" + (i + 1));
                    }
                    else
                    {
                        list2.Add(farmerFromFarmerNumber.dancePartner.name);
                        list4.Remove(farmerFromFarmerNumber.dancePartner.name);
                        list.Add("farmer" + (i + 1));
                    }
                }
            }
            while (list.Count<string>() < 5)
            {
                string text = list3.Last<string>();
                if (list4.Contains(Utility.getLoveInterest(text)))
                {
                    list.Add(text);
                    list2.Add(Utility.getLoveInterest(text));
                }
                list3.Remove(text);
            }
            string text2 = data["mainEvent"];
            for (int j = 1; j <= 5; j++)
            {
                text2 = text2.Replace("Girl" + j, list[j - 1]);
                text2 = text2.Replace("Guy" + j, list2[j - 1]);
            }
            Regex regex = new Regex("showFrame (?<farmerName>farmer\\d) 44");
            Regex regex2 = new Regex("showFrame (?<farmerName>farmer\\d) 40");
            Regex regex3 = new Regex("animate (?<farmerName>farmer\\d) false true 600 44 45");
            Regex regex4 = new Regex("animate (?<farmerName>farmer\\d) false true 600 43 41 43 42");
            Regex regex5 = new Regex("animate (?<farmerName>farmer\\d) false true 300 46 47");
            Regex regex6 = new Regex("animate (?<farmerName>farmer\\d) false true 600 46 47");
            text2 = regex.Replace(text2, "showFrame $1 12/faceDirection $1 0");
            text2 = regex2.Replace(text2, "showFrame $1 0/faceDirection $1 2");
            text2 = regex3.Replace(text2, "animate $1 false true 600 12 13 12 14");
            text2 = regex4.Replace(text2, "animate $1 false true 596 4 0");
            text2 = regex5.Replace(text2, "animate $1 false true 150 12 13 12 14");
            text2 = regex6.Replace(text2, "animate $1 false true 600 0 3");
            string[] array = text2.Split(new char[]
			{
				'/'
			});
            dance.eventCommands = array;
        }
    }
}
