/*

select * from dbo.TransportAudit order by publishtimestamp desc
select * from dbo.TransportAudit order by dequeuetime desc
select * from dbo.TransportAudit order by processtime desc

select * from dbo.TransportAudit 
where messageid in
(select messageid from dbo.TransportAudit 
where Success = 0)
and channel = 'get-invoice-details'
--and success = 0
order by handlerstart desc

*/

/*

drop procedure dbo.spTransportAudit_InsertUpdate
drop table dbo.TransportAudit

*/
