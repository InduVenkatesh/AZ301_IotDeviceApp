using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IoTDeviceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly RegistryManager _registryManager;
        private readonly string _deviceConnectionString;

        public DeviceController(RegistryManager registryManager, string deviceConnectionString)
        {
            _registryManager = registryManager;
            _deviceConnectionString = deviceConnectionString;
        }

        [HttpPost("{deviceId}")]
        public async Task<IActionResult> CreateDevice(string deviceId)
        {
            var device = new Device(deviceId);
            await _registryManager.AddDeviceAsync(device);
            return Ok($"Device {deviceId} created.");
        }

        [HttpPost("telemetry/{deviceId}")]
        public async Task<IActionResult> SendTelemetry(string deviceId, [FromBody] Dictionary<string, object> telemetryData)
        {
            try
            {
                using var deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
                
                var messageString = System.Text.Json.JsonSerializer.Serialize(telemetryData);
                var message = new Microsoft.Azure.Devices.Client.Message(System.Text.Encoding.UTF8.GetBytes(messageString))
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8"
                };

                await deviceClient.SendEventAsync(message);
                return Ok("Telemetry sent successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sending telemetry: {ex.Message}");
            }
        }


        [HttpPut("{deviceId}")]
        public async Task<IActionResult> UpdateDevice(string deviceId)
        {
            var device = await _registryManager.GetDeviceAsync(deviceId);
            if (device == null) return NotFound();
            device.Status = DeviceStatus.Enabled;
            await _registryManager.UpdateDeviceAsync(device);
            return Ok($"Device {deviceId} updated.");
        }

        [HttpPut("desired/{deviceId}")]
        public async Task<IActionResult> UpdateDesiredProperties(string deviceId, [FromBody] Dictionary<string, object> desiredProps)
        {
            try
            {
                var twin = await _registryManager.GetTwinAsync(deviceId);
                var patch = new TwinCollection();
                foreach (var prop in desiredProps)
                {
                    twin.Properties.Desired[prop.Key] = prop.Value;
                }
                await _registryManager.UpdateTwinAsync(deviceId, twin, twin.ETag);
                return Ok("Desired properties updated.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating desired properties: {ex.Message}");
            }
        }

        [HttpPut("reported/{deviceId}")]
        public async Task<IActionResult> UpdateReportedProperties(string deviceId, [FromBody] Dictionary<string, object> reportedProps)
        {
            try
            {
                using var deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
                var reported = new TwinCollection();
                foreach (var prop in reportedProps)
                {
                    reported[prop.Key] = prop.Value;
                }
                await deviceClient.UpdateReportedPropertiesAsync(reported);
                return Ok("Reported properties updated.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating reported properties: {ex.Message}");
            }
        }

        [HttpGet("{deviceId}")]
        public async Task<IActionResult> GetDevice(string deviceId)
        {
            var device = await _registryManager.GetDeviceAsync(deviceId);
            if (device == null) return NotFound();
            return Ok(device);
        }

        [HttpGet("twin/{deviceId}")]
        public async Task<IActionResult> GetDeviceTwin(string deviceId)
        {
            try
            {
                var twin = await _registryManager.GetTwinAsync(deviceId);
                return Ok(twin);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving twin: {ex.Message}");
            }
        }
        

        [HttpDelete("{deviceId}")]
        public async Task<IActionResult> DeleteDevice(string deviceId)
        {
            await _registryManager.RemoveDeviceAsync(deviceId);
            return Ok($"Device {deviceId} deleted.");
        }
    }
}
