# Questions

## How did you approach solving the problem?
I chose to split the file size into 10 equal packets and sent in the range such that all the bytes were received.
After I verified that the files are received to the expectation and the file contents are matching to the original content,
I introduced implementing timespan, file progress and TPL.

## How did you verify your solution works correctly?
I used a text compare tool (Beyond Compare) to check if the whole downloaded file and the part downloaded and assembled file are the same.

## How long did you spend on the exercise?
3-4 hours over a period of 2 weeks

## What would you add if you had more time and how?
I currently chose a way to set the size of the packet to be static and set it from outside of the class.  I would like to get the actual bytes transferred over
a period of time and get the average bytes at that time. 

And I would like this to be a variable quantity and not a constant sized one


