#CALCULATE NUMBER OF PLAYERS PER COUNTRY
SELECT Users.country AS "Country", count(Users.id) AS "User Per Country"
FROM  Users 
GROUP BY Users.Country 