title: Example: One Bowling Throw
// EXAMPLE: the smallest minigame story. One throw with the game's own timing bar, three outcomes.
// @minigame skillcheck 0.5 strike>=0.85 spare>=0.5 gutter
//   skillcheck = the timing bar (click when the marker is in the target). 0.5 = difficulty (0 to 20, optional, right after the type).
//   Then: go to "strike" if the score is at least 0.85, else "spare" if at least 0.5, else "gutter" (the last one, without >=, is the fallback).
// In text: {pins} = points this throw (0-10), {score} = score in percent, {total} = points added up (see Strike Night: The Gauntlet for @score / @total).
// If the game has not loaded its timing bar yet, the throw is a random roll and the log says so. Doing one work shift first loads it.
// Without tools\export_backgrounds.py the bowling alley is a simple drawn stand-in (jun_backgrounds\bowling.png).

# start
@background bowling
@jun -0.8 1.4
{bot} [lecture]: Test throw. Click when the marker is inside the target. I will record the result.
@minigame skillcheck 0.5 strike>=0.85 spare>=0.5 gutter

# strike
{bot} [surprised]: Strike! {pins} pins, score {score}.
-> result

# spare
{bot} [happy]: Not bad. {pins} pins, score {score}.
-> result

# gutter
{bot} [sad]: {pins} pins. The ball and the gutter are now friends. Score {score}.
-> result

# result
{bot} [lecture]: Total so far: {total} pins. Test complete.
-> end
