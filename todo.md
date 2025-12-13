
- [x] Show by permission scripts in players backend page
- [ ] Change how the odd and even row images get enabled and disabled
- [ ] Prevent changing the last player which has permissions to edit permissions to a group which does not have those permissions
- [ ] Use a selectable or a toggle for the highlight shown while the permission group popup is shown
- [x] When losing permissions to edit one of the columns in the player backend, but that was the active sort order, change sort order to the player name
- [ ] Maybe actually just disable (make non interactable) the 3 columns that are tied to permissions in the players backend page rather than hiding them, except for the delete column that would go hidden
- [ ] Should there be an indicator for when a player is online or offline in the backend page outside of the delete button being greyed out?
- [x] Check permissions for all interactions in the players backend manager, when running the IAs. Raise events in case an action got ignored so external latency states can be reset

- [ ] How to truly prevent locking yourself out of permissions to edit permissions
  - editing the permission values themselves could lock you out
  - deleting a permission group could lock you out
  - deleting the last player data which had the necessary permissions could lock you out
  - changing the last player data's group could lock you out
