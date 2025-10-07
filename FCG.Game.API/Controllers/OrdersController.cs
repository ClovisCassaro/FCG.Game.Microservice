using FCG.Game.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Game.API.Controllers;

[Authorize]
public class OrdersController : ApiBaseController
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var userId = GetUserId();
            var orderId = await _orderService.CreateOrderAsync(userId, request.Items);

            return CreatedAtAction(nameof(GetOrder), new { id = orderId }, new { orderId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteOrder(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var success = await _orderService.CompleteOrderAsync(id, userId);

            if (!success)
                return NotFound();

            return Ok(new { message = "Pedido completado com sucesso" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var orders = await _orderService.GetUserOrdersAsync(userId, page, pageSize);

        return Ok(new
        {
            page,
            pageSize,
            total = orders.Count,
            data = orders
        });
    }
}

public record CreateOrderRequest(List<OrderItemRequest> Items);