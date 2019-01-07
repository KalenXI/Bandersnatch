Original Reddit Post: https://www.reddit.com/r/blackmirror/comments/adbppu/reverse_engineering_the_bandersnatch_state_engine/

# Reverse Engineering the Bandersnatch State Engine

So I decided to spend the weekend reverse engineering the state engine that Netflix uses to drive their interactive videos. Unfortunately it got to the point where the boolean branching operations got too complex to do manually. So I figured I'd just present what I have so far in case anybody wants to pick up where I left off.

The repository with my work so far is here: [https://github.com/KalenXI/Bandersnatch](https://github.com/KalenXI/Bandersnatch)

It contains a program written in C# that will take the Bandersnatch JSON data and process it in a variety of ways. It's what I used to generate the 3 text files in the root of the repository:

* Bandersnatch.json - This is the original JSON metadata pulled from Netflix that defines all of the scenes and the direction each decision will take.
* Moments.txt - This is a list of all scenes alphabetically including which state variable get set once that scene is entered, the destination scene or scene group for each option, and any self-contained preconditions for entering that scene.
* States.txt - This is a list of every state variable, which scenes set that variable, and to what. Also includes some notes for what choice each state represents. A lot of them just indicate whether a certain scene has been seen.
* Preconditions.txt - This is a list of the global branching preconditions for every scene that has them. This is what the branching engine uses to chose which scene to go to when sent to a sequence group.

I also started to make two flow charts defining the possible paths. Page 1 is a non-linear representation and page 2 is a very incomplete linear representation: [https://www.draw.io/?lightbox=1&highlight=0000ff&edit=\_blank&layers=1&nav=1&page=1&title=Bandersnatch.xml#Uhttps%3A%2F%2Fdrive.google.com%2Fuc%3Fid%3D1NKu-ES-ZXVRLN5cr1D1YEKZwZJGaYbo6%26export%3Ddownload](https://www.draw.io/?lightbox=1&highlight=0000ff&edit=_blank&layers=1&nav=1&page=1&title=Bandersnatch.xml#Uhttps%3A%2F%2Fdrive.google.com%2Fuc%3Fid%3D1NKu-ES-ZXVRLN5cr1D1YEKZwZJGaYbo6%26export%3Ddownload)

The non-linear representation breaks at each scene group and you'd have to go through the list of preconditions to determine which path would have been followed given the current state of the state variables. The linear representation was my attempt to actually follow all of the possible choices. You can see I only got partly down a single path before I started running into boolean operators for the precondition that looked like this: (((!(p\_cm || p\_ty) && !(p\_lsd && p\_2b) && (!(p\_2b) || (p\_2b && p\_lsd)) && p\_3aj) || (!(p\_cm || p\_ty) && !(p\_lsd && p\_2b) && p\_2b && !(p\_lsd) && p\_3ah) || (p\_cm || p\_ty)) && !(p\_3ab || p\_3ac || p\_3aj || p\_3ah || p\_3ak || p\_3al || p\_3af)). At that point it was taking me 15 minutes per branch just to manually translate the prefix boolean JSON notation used in the metadata to the infix boolean notation that Javascript and C# used just to figure out which branch to take so I stopped.

In the flow chart:

* Rectangles = Sequences, including title, sequence ID, and any state variable that are set by viewing said sequence.
* Ovals = Choice options branching from the preceding sequence
* Cylinders = Sequence groups. These are the major components of how the branching engine works. When a choice leads to a sequence group the engine will go down the options one by one comparing the current settings of the state variables to the preconditions for each sequence listed in the sequence group. Once it finds one that returns true it will follow that path.
* Hexagons = Precondition groups. Once you get to a sequence group you go down through the hexagons from top to bottom until you find a set of preconditions that matches the current state variable state at which point you should branch to the right and jump to that particular sequence.

Hopefully someone else finds this interesting as well. The state engine that Netflix built for their interactive stories is amazingly flexible. As you can see from the flow chart there are some scenes that can branch in 12+ different directions depending on exactly what choices the user made. There's (if I'm remembering right) 143 sequences with 2 choices each which gives you a theoretical maximum of 2^(143) possible paths. In practice there are fewer paths because some dead end and some loop back on themselves. There are 2^(59)ish or  576 quadrillion possible state engine states with 62 variables (again not all of which are used in practice).
