// #autoload
// #name = DoshFriendlyWaypoints
// #version = 2.0
// #date = May 15, 2025
// #warrior = ilDosh
// #description = Creates waypoints to all friendly players with a keypress toggle. Please assign a keybind to toggle it on or off. Note: T2's waypoint array has a max size of 16, so only up to 16 players can be waypointed.
// #category = Game Enhancement

// Create a simset to keep track of our waypoints
if(!isObject($FriendlyWaypoints))
    $FriendlyWaypoints = new SimSet();

// Initialize variables
$FriendlyWaypoints::Active = false;
$FriendlyWaypoints::PrefsFile = "prefs/DoshFriendlyWaypoints.cs";

// Load preferences
function FriendlyWaypoints::loadPrefs()
{
    if(isFile($FriendlyWaypoints::PrefsFile))
    {
        exec($FriendlyWaypoints::PrefsFile);
    }
}

// Save preferences
function FriendlyWaypoints::savePrefs()
{
    export("$FriendlyWaypoints::Act*", $FriendlyWaypoints::PrefsFile, false);
}

// Register the callback for team updates
function FriendlyWaypoints::onInitCallbacks()
{
    // Whenever any player joins a team, update if it's us or our teammates
    Callback.add(TeamUpdated, "FriendlyWaypoints::handleTeamUpdate");
}

// Define the function to be called when key is pressed - will toggle waypoints
function FriendlyWaypointsToggle(%val)
{
    // Only execute on key press (not release)
    if (%val)
    {
        $FriendlyWaypoints::Active = !$FriendlyWaypoints::Active;
        
        if($FriendlyWaypoints::Active)
        {
            FriendlyWaypoints::markAllFriendlies();
            addMessageHudLine("\c2Friendly Waypoints: \c3Enabled");
        }
        else
        {
            FriendlyWaypoints::clearWaypoints();
            addMessageHudLine("\c2Friendly Waypoints: \c1Disabled");
        }
        
        // Save preference
        FriendlyWaypoints::savePrefs();
    }
}

// Main package
package FriendlyWaypoints {

    // Function to mark all friendly players
    function FriendlyWaypoints::markAllFriendlies()
    {
        // First, clear any existing friendly waypoints
        FriendlyWaypoints::clearWaypoints();
        
        // Get the player's team
        %myTeam = teamTracker.friendlyTeamID;
        
        // Find all players on the same team
        %teamPlayers = teamTracker.teamGroup[%myTeam];
        if(!isObject(%teamPlayers))
        {
            addMessageHudLine("\c2Could not find your team.");
            return;
        }
        
        %totalTeamCount = %teamPlayers.getCount();
        %friendlyCount = 0;
        %myID = PlayerList.getMyID();

        // Loop through all friendly players
        for(%i = 0; %i < %totalTeamCount; %i++)
        {
            %player = %teamPlayers.getObject(%i);
            echo(%player.name);
            
            // Skip if it's our player
            if(%player.clientId == %myID)
                continue;
       
            echo("isObject(%player) == " @ isObject(%player));
            echo("%player.targetId == " @ %player.targetId @ ". %player.targetId $= '' == " @ (%player.targetId $= ""));
            echo(" %player.targetId == " @  %player.targetId @ ".  %player.targetId == 0 == " @ (%player.targetId == 0));

            echo("Player client ID from ID " @ PlayerList.getTargetIDByID(%player.clientId) @ ". " @ %player.clientId);
            // Skip if player has no valid targetId
            if(!isObject(%player) || %player.targetId $= "" || %player.targetId == 0)
                continue;
 
            // Create a waypoint for this player
            %target = createClientTarget(%player.targetId, "0 0 0");
            if(!isObject(%target))
                continue;
            
            // Get the player's name
            %name = %player.name;
            if(%name $= "")
                %name = "Friendly " @ %friendlyCount;
                
            // Create the waypoint
            %target.createWaypoint(%name);
            // Store the target for later cleanup
            $FriendlyWaypoints.add(%target);
            %friendlyCount++;
        }
    }
    
    // Function to clear all friendly waypoints
    function FriendlyWaypoints::clearWaypoints()
    {
        %count = $FriendlyWaypoints.getCount();
        for(%i = 0; %i < %count; %i++)
        {
            %target = $FriendlyWaypoints.getObject(0);
            // Remove the waypoint from the target
            if(isObject(%target))
            {
                // Delete the target
                %target.delete();
            }
        }
        $FriendlyWaypoints.clear();
    }
    
    // Clean up on mission end
    function GameConnection::onMissionEnded(%this)
    {
        parent::onMissionEnded(%this);
        FriendlyWaypoints::clearWaypoints();
    }
    
    // Hook into TeamUpdated callback for updates to team
    function FriendlyWaypoints::handleTeamUpdate(%teamID, %clientID)
    {
        %myTeam = teamTracker.friendlyTeamID;
        if (%myTeam <= 0)
        {
            return;
        }
        if (%teamID == teamTracker.friendlyTeamID || %clientID == PlayerList.getMyID())
            FriendlyWaypoints::markAllFriendlies();
    }

    function OptionsDlg::onWake(%this)
    {
        if (!$DoshFriendlyWaypoints::optionsAdded) {
            $RemapName[$RemapCount] = "Dosh-Toggle Waypoints";
            $RemapCmd[$RemapCount] = "FriendlyWaypointsToggle";
            $RemapCount++;
            $DoshFriendlyWaypoints::optionsAdded = true;
        }
        parent::onWake(%this);
    }
    
    // Hook into the disconnect function properly
    function disconnect()
    {
        // Clear waypoints before disconnecting
        FriendlyWaypoints::clearWaypoints();
        
        // Call the parent disconnect function properly
        Parent::disconnect();
    }
};

FriendlyWaypoints::onInitCallbacks();
FriendlyWaypoints::loadPrefs();
activatePackage(FriendlyWaypoints);