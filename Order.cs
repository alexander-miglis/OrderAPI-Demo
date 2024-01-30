using Amazon.DynamoDBv2.DataModel;

namespace OrderAPI;
[DynamoDBTable("OrderTable")]
public class Order
{
	public string OrderId { get; set; }
	public decimal Total { get; set; }
	public DateTime CreatedDate { get; set; }
}