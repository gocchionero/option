# option
A non-quant library written to facilitate dealing with standardized options.

As per widespread modern financial code development customs, object-oriented code is written is C# while more quantitative modules are written in F#. Here you will find functions to compute both past and future option expiration dates, to build strike and options chains for both calls and puts and, most of all, to easily get the option symbol via the ToString() method.

The need for the underlying stock symbol in the constructor comes from the need to leave the door open to easily interface this library with the one I have published (or others) to Math.Net Numerics for pull, which is in F# and focuses instead on quant matters:

https://github.com/mathnet/mathnet-numerics/pull/599

It shouldn't therefore be difficult to link this code to your preferred quote-provider library, and eventually also get theoretical prices and greeks for your option chains.

So, basically, with all the tools above, we should at least be one step closer to options.

Giulio Occhionero,
October 2018
