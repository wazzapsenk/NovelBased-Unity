using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nullframes.Intrigues.Demo {
    public class Demo_Methods : MonoBehaviour {
        [Note(
            "This script file contains methods that <b>Intrigues</b> can call.\nThese methods, as shown in the example, should be located within a <b>single-instance</b> class.\n\nFor detailed information, refer to the <b>documentation</b> and tutorial videos.")]
        public bool ok;

        [IInvoke]
        private IResult Raid(Scheme scheme) {
            if ( !IM.IsPlayer(scheme.Schemer.Conspirator) ) return IResult.False;

            NInt coins = scheme.Schemer.Conspirator.GetVariable< NInt >("Coin");
            int amount = Random.Range(45, 85);

            coins.Value += amount;

            NotificationSystem.ShowNotification($"You gained <color=#9BFF78>{amount}</color> coins from the raid.");
            return IResult.True;
        }

        [IInvoke("Age Check")]
        private IResult AgeCheck(Actor conspirator, Actor target) {
            if ( conspirator == null || target == null ) return IResult.Null;

            // Checks the age difference. Returns False if the difference is greater than 7.
            if ( Math.Abs(conspirator.Age - target.Age) > 7 ) {
                return IResult.False;
            }
            
            return IResult.True;
        }

        [GetFamily("King's Family")]
        private Family KingsFamily(Scheme scheme) {
            if ( scheme.Schemer.Conspirator.Clan == null ) return null;
            
            Actor king = scheme.Schemer.Conspirator.Clan.GetMember("Leader");
            if(king == null) return null;

            return king.Family;
        }

        [IInvoke("Conspirator Heir Became General")]
        private IResult ConspiratorBecameGeneral(Scheme scheme) {
            if ( scheme.Schemer.Conspirator.Heir != null ) {
                scheme.Schemer.Conspirator.Heir.JoinClan(scheme.Schemer.Conspirator.Clan);
                scheme.Schemer.Conspirator.Heir.SetRole("General");
                return IResult.True;
            }

            return IResult.False;
        }

        [IInvoke("Target Heir Became General")]
        private IResult TargetBecameGeneral(Scheme scheme) {
            if ( scheme.Schemer.Target.Heir != null ) {
                scheme.Schemer.Target.Heir.JoinClan(scheme.Schemer.Target.Clan);
                scheme.Schemer.Target.Heir.SetRole("General");
                return IResult.True;
            }

            return IResult.False;
        }

        [GetActor("Get Leader")]
        private Actor GetLeader(Scheme scheme) {
            return scheme.Schemer.Conspirator.Clan?.GetMember("Leader");
        }

        [GetActor("Get Target Heir")]
        private Actor FindTargetHeir(Scheme scheme) {
            return scheme.Schemer.Target.Heir;
        }

        [GetActor("Get Conspirator Heir")]
        private Actor FindConspiratorHeir(Scheme scheme) {
            return scheme.Schemer.Conspirator.Heir;
        }
    }
}