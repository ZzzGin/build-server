/////////////////////////////////////////////////////////////////////
// RdRqBlockingQueue.cs                                            //
// ver 1.0                                                         //
// Jing Qi                                                         //
/////////////////////////////////////////////////////////////////////

/*
 * This package bases on the BlockingQueue.
 * It provides a blocking queue of two blocking queue (request pool & ready pool)
 * You can enqueue someting into one of them, but, it can only deq when request 
 * pool and ready pool both have elements.
 * 
 * Functions in Class:
 *  -enrqQ()    : enqueue a request into RdRqBlockingQueue
 *  -enrdQ()    : enqueue a ready into RdRqBlockingQueue
 *  -deQ()      : deQ a request and a ready from RdRqBlockingQueue
 *  -rqSize()   : count the size of request queue
 *  -rdSize()   : count the size of ready queue
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 15 Jun 2017
 * - first release
 */

using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SWTools;

namespace ProcessPool
{
    public class RdRqBlockingQueue<T1, T2>
    {
        // there are two queue, one is reqeustQueue-rqQ, the other one is readyQueue-rdQ
        public static BlockingQueue<T1> rqQ { get; set; } = null;
        public static BlockingQueue<T2> rdQ { get; set; } = null;
        object locker_ = new object();

        // Constructor
        public RdRqBlockingQueue()
        {
            if (rqQ == null)
                rqQ = new BlockingQueue<T1>();
            if (rdQ == null)
                rdQ = new BlockingQueue<T2>();
        }

        // insert an element into requestQueue
        public void enrqQ(T1 request)
        {
            lock (locker_)
            {
                rqQ.enQ(request);
                Monitor.Pulse(locker_);
            }
        }

        // insert an element into readyQueue
        public void enrdQ(T2 ready)
        {
            lock (locker_)
            {
                rdQ.enQ(ready);
                //Console.Write("\nrdQ+1");
                Monitor.Pulse(locker_);
            }
        }

        // deQueue from the queues as a whole. get one request and one ready at one time.
        // it would be locked when one of sub-queues' size is 0.
        public void deQ(out T1 request, out T2 ready)
        {
            lock (locker_)
            {
                while (this.rdSize() == 0 || this.rqSize() == 0)
                {
                    Monitor.Wait(locker_);
                }
                request = rqQ.deQ();
                ready = rdQ.deQ();
            }
        }

        // get the size of requestqueue
        public int rqSize()
        {
            int count;
            lock (locker_) { count = rqQ.size(); }
            return count;
        }

        // get the size of readyQueue
        public int rdSize()
        {
            int count;
            lock (locker_) { count = rdQ.size(); }
            return count;
        }
    }

    // Main for testing.
    class Program
    {
        static void Main(string[] args)
        {
            RdRqBlockingQueue<string, int> q = new RdRqBlockingQueue<string, int>();
            Thread t = new Thread(() =>
            {
                string request;
                int ready;
                while (true)
                {
                    q.deQ(out request, out ready);
                    Console.Write("\n  child thread received request {0}", request);
                    Console.Write("\n  child thread received ready {0}", ready);
                    if (request == "quit") break;
                }
            });
            t.Start();
            string sendRequest = "request #";
            for (int i = 0; i < 20; ++i)
            {
                string tempRequest = sendRequest + i.ToString();
                Console.Write("\n  main thread sending request {0}", tempRequest);
                q.enrqQ(tempRequest);
            }
            for (int i = 0; i < 20; ++i)
            {
                Console.Write("\n  main thread sending ready {0}", i);
                q.enrdQ(i);
            }
            q.enrqQ("quit");
            q.enrdQ(100);
            t.Join();
            Console.Write("\n\n");
        }
    }
}


