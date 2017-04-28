# Automatic Stroke Assessment System (AUROS)

WPF Application that capture stroke patient's motion data and score it based on Fugl-Meyer Assessment System. 

## Getting Started

Pull at https://github.com/rjjatson/auro.git 
open the solution using Visual Studio. VS 2015 is recommended. 

## Prerequisites

*Installed Kinect 2.0 for windows (could be replaced by dummy input)

*Composite hand glove sensor (could be replaced by dummy input)

*Presentation pointer- pgUp and pgDown emulated

*Kinect 2.0 SDK

*Internet connection

## Deployment

Deploy this project on Windows 10 OS.
This project consists of 3 sub systems : 

*Auros - WPF : Capturing Function

*Feature Extractor - Console : Extracting features of raw data for machine learning algorithms

*Regressor Compare - Console : Fed the extracted data into Defined Azure Web Service. This service comparing performance of 5 machine learning regression algorithms : bayesian linear, linear, NN, Boosted Decision tree, decision forestgiven input in very specified format (form preproc program).
! note : feel free to consume my azure web service but transaction limit may apply. 

## Running

You need to check your isolated storage for each process output typically located on C:\Users\<**yourname**\AppData\Local\IsolatedStorage

*Auros : 
set up the input modules : place kinect sensor infront of subject and wear the composite glove. if it's not avalabe , you may use dummy input function by clicking dummy button on upper right screen. 
press pgDn/ pointer presentation/ click the button on lower right screen, follow the recording instruction. the raw data wil be available on the aforementioned isoStorage. check raw data folder on isoStorage.

*Feature Extractor : make sure raw data is avaliable, run the program. check preproc folder on isoStorage.

*Regressor Compare : make sure preproc data available. it will fed the data in my azure storage to azure machine learning. you may **not** modify the storage. but, you could define your own data input from different storage for the ML. 

## Improvement
this project demonstrates the **TRAINING** function of azure machine learning, i will add the function of **SCORING** in near future.
i am publishing research paper for this project, i will add the abstract, and you may ask for the full text soon.

## Authors

**Ricky Julianjatsono** - *Initial work*
