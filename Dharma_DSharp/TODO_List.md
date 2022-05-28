# TODO-List  

Sorted by priority. Should be put somewhere else in the future.

## Database
We need a place to store some information, a database would be a great place to do that. TBD which database to use, ask Megu first

## Partying system  
### Logic:  
=> scan alert for "uq in x minutes"
=> Partying up message
 edit the message with each new uq? => don't do this as users won't be notified about new uqs that way
 and after each uq, clear reactions, clear all banter (kinda like an automatic purge)? 
 => message has 2 reactions, join and leave (if someone just wants to try out they can leave again or if something comes around)

### Partying-channel:  
=> Dharma Bot posts 1 partying up message  
=> Users may talk in there? The bot could clean all banter after each uq 

## Sticky roles
Sticky roles would bypass discord in-built membership screening, so only people that passed the membership screening should
get their old roles re-assigned.

The bot needs to listen to "guild rules updated" =>
1. Create a blacklist for sticky-rules for all members that joined before the event got fired
2. Don't process the blacklisted members in sticky-role functions

## Activity-check:
All @ArksOperatives will be sent a dm with a reaction to say that they're active
All @ArksOperatives that can't be send a dm will be !!!TBD!!!

### Website approach
Create a website that displays the data
- Integrate discord authentication process for security reasons

### Google Drive approach (would be urgh)
[G-Drive]()

## Level System
Todo inside of program ^-^

## Voice-System
Test Lavalink