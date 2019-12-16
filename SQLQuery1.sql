

--insert into ProductStock values(1,3,0)
select * from ProductStock
select * from Stockist

select *
from ProductStock ps join Stockist s 
on ps.StockID=s.StockistID
where ps.ProductReferenceID=1 