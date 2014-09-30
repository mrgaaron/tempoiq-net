﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TempoIQ.Json;
using TempoIQ.Models;
using TempoIQ.Results;
using TempoIQ.Queries;
using TempoIQ.Utilities;
using Newtonsoft.Json;
using NodaTime;

namespace TempoIQ
{
    /// <summary>
    /// The Client is the primary interface with TempoIQ
    /// </summary>
    public class Client
    {
        /// <summary> Handles the actual network operations </summary>
        private Executor Runner { get; set; }

        public const string API_VERSION1 = "v1";
        public const string API_VERSION2 = "v2";
        public string API_VERSION { get; private set; }

        /// <summary>
        /// Create a new client from credentials, backend, port(optional) and timeout(optional, in milliseconds)
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        public Client(Credentials credentials, string host, int port = 443, int timeout = 50000)
        {
            API_VERSION = API_VERSION2;
            var builder = new UriBuilder
            {
                Scheme = "https",
                Host = host,
                Port = port
            };
            Runner = new Executor(builder.Uri, credentials, timeout);
        }

        /// <summary>
        /// Create a new device
        /// </summary>
        /// <param name="device"></param>
        /// <returns>a Result with the created Device</returns>
        public Result<Device> CreateDevice(Device device)
        {
            string target = String.Format("{0}/devices/", API_VERSION);
            return Runner.Post<Device>(target, device);
        }
        
        /// <summary>
        /// Retrieve a device of a given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>a Result with the device of that key, if any</returns>
        public Result<Device> GetDevice(string key)
        {
            var target = String.Format("{0}/devices/{1}/", API_VERSION, HttpUtility.UrlEncode(key));
            return Runner.Get<Device>(target);
        }

        /// <summary>
        /// Replace a device
        /// </summary>
        /// <param name="device"></param>
        /// <returns>a Result with the updated Device</returns>
        public Result<Device> UpdateDevice(Device device)
        {
            var target = String.Format("{0}/devices/{1}/", API_VERSION, HttpUtility.UrlEncode(device.Key));
            return Runner.Put<Device>(target, device);
        }

        /// <summary>
        /// List the devices which meet a given selection
        /// </summary>
        /// <param name="selection"></param>
        /// <returns>a result with the selected Devices</returns>
        public Result<Cursor<Device>> ListDevics(Selection selection)
        {
            var target = String.Format("{0}/devices/query/", API_VERSION);
            var query = new FindQuery(
                new Search(Select.Type.Devices, selection),
                new Find());
            var prelim = Runner.Post<Segment<Device>>(target, query);
            return prelim.ToCursorResult<Device>();
        }

        /// <summary>
        /// Delete a device of a given key
        /// </summary>
        /// <param name="device"></param>
        /// <returns>a Result with the success or failure of the operation only</returns>
        public Result<Unit> DeleteDevice(Device device)
        {
            var target = String.Format("{0}/devices/{1}/", API_VERSION, HttpUtility.UrlEncode(device.Key));
            var result = Runner.Delete<Unit>(target);
            return result;
        }

        /// <summary>
        /// Delete the devices which meet a given selection
        /// </summary>
        /// <param name="selection"></param>
        /// <returns>a Result with the success or failure of the operation only</returns>
        public Result<DeleteSummary> DeleteDevices(Selection selection)
        {
            var target = String.Format("{0}/devices/", API_VERSION);
            var query = new FindQuery(new Search(Select.Type.Devices, selection), new Find());
            return Runner.Delete<DeleteSummary>(target, query);
        }

        /// <summary>
        /// Delete all devices
        /// </summary>
        /// <returns>a Result with the success or failure of the operation only</returns>
        public Result<DeleteSummary> DeleteAllDevices()
        {
            var allSelection = new Selection().Add(Select.Type.Devices, new AllSelector());
            return DeleteDevices(allSelection);
        }

        /// <summary>
        /// Write datapoints with a MultiDataPoint
        /// </summary>
        /// <param name="device"></param>
        /// <param name="data"></param>
        /// <returns>a Result with the success or failure of the operation only</returns>
        public Result<Unit> WriteDataPoints(Device device, MultiDataPoint data)
        {
            var writeRequest = new WriteRequest();
            foreach(var pair in data.vs)
                writeRequest.Add(device.Key, pair.Key, new DataPoint(data.t, pair.Value));
            return WriteDataPoints(writeRequest);
        }

        /// <summary>
        /// Write datapoints with a List<MultiDataPoint>
        /// </summary>
        /// <param name="device"></param>
        /// <param name="data"></param>
        /// <returns>a Result with the success or failure of the operation only</returns>
        public Result<Unit> WriteDataPoints(Device device, IList<MultiDataPoint> data)
        {
            var writeRequest = data.Aggregate(new WriteRequest(),
                (acc, mdp) => mdp.vs.Aggregate(acc,
                    (req, pair) => req.Add(device.Key, pair.Key, new DataPoint(mdp.t, pair.Value))));
            var result = WriteDataPoints(writeRequest);
            return result;
        }

        /// <summary>
        /// Write data to a given sensor on a given device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="sensor"></param>
        /// <param name="data"></param>
        /// <returns>a Result with the success or failure of the operation only</returns>
        public Result<Unit> WriteDataPoints(Device device, Sensor sensor, IList<DataPoint> data)
        {
            var result = WriteDataPoints(device.Key, sensor.Key, data);
            return result;
        }

        /// <summary>
        /// Write data to a given sensor on a given device
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <param name="sensorKey"></param>
        /// <param name="data"></param>
        /// <returns>a Result with the success or failure of the operation only</returns>
        public Result<Unit> WriteDataPoints(string deviceKey, string sensorKey, IList<DataPoint> data)
        {
            var writeRequest = data.Aggregate(new WriteRequest(),
                (req, dp) => req.Add(deviceKey, sensorKey, dp));
            var result = WriteDataPoints(writeRequest);
            return result;
        }

        /// <summary>
        /// Write data from a WriteRequest object
        /// </summary>
        /// <param name="writeRequest"></param>
        /// <returns>a Result with the success or failure of the operation only</returns>
        public Result<Unit> WriteDataPoints(WriteRequest writeRequest)
        {
            var target = String.Format("{0}/write/", API_VERSION);
            var result =  Runner.Post<Unit>(target, writeRequest);
            return result;
        }

        /// <summary>
        /// Read data from a selection, start time and stop time
        /// </summary>
        /// <param name="selection"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns>The data from the devices and sensors which match your selection, 
        /// as processed by the pipeline, and bookended by the start and stop times</returns>
        public Result<Cursor<Row>> Read(Selection selection, ZonedDateTime start, ZonedDateTime stop)
        {
            var search = new Search(Select.Type.Sensors, selection);
            var read = new Read(start, stop);
            var query = new ReadQuery(search, read);
            return Read(query);
        }

        /// <summary>
        /// Read data from a Selection, function pipeline, start time and stop time
        /// </summary>
        /// <param name="selection"></param>
        /// <param name="pipeline"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns>The data from the devices and sensors which match your selection, 
        /// as processed by the pipeline, and bookended by the start and stop times</returns>
        public Result<Cursor<Row>> Read(Selection selection, Pipeline pipeline, ZonedDateTime start, ZonedDateTime stop)
        {
            var query = new ReadQuery(new Search(Select.Type.Sensors, selection), new Read(start, stop), pipeline);
            return Read(query);
        }

        /// <summary>
        /// Read data from a ReadQuery
        /// </summary>
        /// <param name="query"></param>
        /// <returns>The data from the devices and sensors which match your selection, 
        /// as processed by the pipeline, and bookended by the start and stop times</returns>
        public Result<Cursor<Row>> Read(ReadQuery query)
        {
            var target = String.Format("{0}/read/query/", API_VERSION);
            return Runner.Post<Segment<Row>>(target, query).ToCursorResult<Row>();
        }
    }

    public static class SegmentCursorTableConversion
    {
        /// <summary>
        /// Extension method to transform Results 
        /// to Result<Cursor>s from Result<Segment>/s
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns>An Result wrapping the cursor equivalent to the 
        /// Segment in the original's Value</returns>
        public static Result<Cursor<T>> ToCursorResult<T>(this Result<Segment<T>> result)
        {
            Cursor<T> cursor;
            if (result.Value == null)
                cursor = new Cursor<T>(new List<Segment<T>>());
            else
                cursor = new Cursor<T>(new List<Segment<T>> { result.Value });
            return new Result<Cursor<T>>(cursor, result.Code, result.Message, result.MultiStatus);
        }
    }
}