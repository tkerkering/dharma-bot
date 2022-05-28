namespace Dharma_DSharp.Constants
{
    public static class DharmaConstants
    {
        public static readonly ulong GuildId = 551594114454519828;

        public static class ChannelIds
        {
            public static readonly ulong StartHere = 879816146365345863;
            public static readonly ulong WelcomeHall = 879484918411984906;
            public static readonly ulong GeneralReactionRoles = 884979772109750272;
            public static readonly ulong PhantasyNgsAlert = 729974331505115157;
            public static readonly ulong PartyingChannel = 979925772733136916;
        }

        public static class RoleIds
        {
            // Administrative officers
            public static readonly ulong Dreamer = 743659766064349264;
            public static readonly ulong ExecutiveOfficer = 752244674726723644;
            public static readonly ulong DiscordOfficer = 703775448001151078;
            public static readonly ulong ArtDirector = 874192921958711306;

            // Non administrative officers
            public static readonly ulong Recruiter = 787349884999041044;
            public static readonly ulong SupportOfficer = 754836832268582932;

            public static readonly List<ulong> AdministrativeOfficers = new()
            {
                Dreamer,
                ExecutiveOfficer,
                DiscordOfficer,
                ArtDirector
            };

            public static readonly List<ulong> AllOfficers = new()
            {
                Dreamer,
                ExecutiveOfficer,
                DiscordOfficer,
                ArtDirector,
                Recruiter,
                SupportOfficer
            };

            // The Dharma alliance member role id.
            public static readonly ulong ArksOperative = 703775060833599599;

            // The Dharma community member role id.
            public static readonly ulong HomieId = 729777643565744240;
        }
    }
}
