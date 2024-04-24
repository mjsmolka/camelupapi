using CamelUpAutomation.Enums;

namespace CamelUpAutomation.Models.Game
{
    public class GameAction
    {
        public string id { get; set; }
        public int Turn { get; set; }
        public int Round { get; set; }
        public string GameId { get; set; }
        public PlayerAction PlayerAction { get; set; }

        // actionData
        public SpectatorTilePlacement SpectatorTilePlacement { get; set; }
        public Partnership Partnership { get; set; }
        public DiceRoll DiceRoll { get; set; }
        public RaceBet RaceBet { get; set; }
        public LegBet LegBet { get; set; }
    }

    public class SpectatorTilePlacement
    {
        public string id { get; set; }
        public string PlayerId { get; set; }
        public int Position { get; set; }
        public CheerTileMode Mode { get; set; }
    }

    public class Partnership
    {
        public string id { get; set; }
        public string PartnerOneId { get; set; }
        public string PartnerTwoId { get; set; }
    }

    public class PatnershipPayOut
    {
        public string id { get; set; }
        public string PartnershipId { get; set; }
        public int WinAmount { get; set; }

    }

    public class DiceRoll
    {
        public string id { get; set; }
        public string PlayerId { get; set; }
        public int RollNumber { get; set; }
        public string SpectatorTileId { get; set; }
        public CamelColor CamelColor { get; set; }
        public bool IsFinal { get; set; }
    }

    public class RaceBet
    {
        public string id { get; set; }
        public string PlayerId { get; set; }
        public CamelColor CamelColor { get; set; }
        public bool IsWinnerBet { get; set; }
    }

    public class LegBet
    {
        public string id { get; set; }

        public string PlayerId { get; set; }

        public string BettingTicketId { get; set; }
    }

    public enum CheerTileMode
    {
        Cheer,
        Boo
    }
}
