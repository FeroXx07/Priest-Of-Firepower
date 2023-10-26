#CALCULATE AVERAGE SESSION TIME
SELECT AVG(TIMESTAMPDIFF(SECOND, Sessions.loginStamp, Sessions.logoutStamp))/60 
AS "Average playTime" 
FROM Sessions, Users 
WHERE Sessions.userId = Users.id
