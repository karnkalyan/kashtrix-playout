// Utility function to split text into spans
function splitTextToSpans(selector) {
  const elements = document.querySelectorAll(selector);
  elements.forEach((element) => {
      const text = element.textContent.trim();
      element.innerHTML = ""; // Clear existing text
      
      // Split text into words while preserving spaces
      const words = text.split(/(\s+)/).filter(word => word.length > 0);
      
      words.forEach((word) => {
          // Create word container
          const wordSpan = document.createElement('span');
          wordSpan.style.display = 'inline-block';
          wordSpan.style.whiteSpace = 'nowrap'; // Prevent word internal breaking
          
          // Split word into characters
          word.split("").forEach((char) => {
              const charSpan = document.createElement("span");
              charSpan.style.display = 'inline-block';
              charSpan.textContent = char === " " ? '\u00A0' : char;
              wordSpan.appendChild(charSpan);
          });
          
          element.appendChild(wordSpan);
      });
  });
}


// Utility function to play custom text animation using GSAP
function playAnimation(elementClass, animationName, delay, callback) {
    const element = document.querySelector(elementClass);
    const chars = element ? element.querySelectorAll("span span") : null;

    if (!chars) return;

    // Stop any ongoing animation on these characters
    gsap.killTweensOf(chars);

    // Reset styles to avoid residual effects
    gsap.set(chars, { opacity: 0, x: 0, y: 0, scale: 1, rotation: 0 });

    const { in: inAnimation, out: outAnimation } = animations[animationName];

    // Run "in" animation
    inAnimation(chars).then(() => {
        setTimeout(() => {
            // Run "out" animation
            outAnimation(chars).then(() => {
                // Animation cycle complete, invoke the callback
                if (callback && typeof callback === 'function') {
                    callback();
                }
            });
        }, delay); // Delay between in and out animations
    });
}

// Function to animate text with a given animation
function animateTextContent(textSelector, textAnimationName, options = {}) {
  const { loop = false, delay = 5000, onComplete } = options;

  // Ensure text is split into spans
  splitTextToSpans(textSelector);

  function runAnimation() {
      playAnimation(textSelector, textAnimationName, delay, () => {
          if (loop) {
              runAnimation(); // Loop animation if enabled
          } else {
              console.log("Animation completed!");
              if (typeof onComplete === 'function') {
                  onComplete();
              }
          }
      });
  }

  runAnimation();
}


// Ticker function with adjustable delay
function createTicker({
  apiUrl,
  textSelector = '.animated-text',
  canvasSelector = '#animationCanvas',
  textAnimationName,
  dataSourceKey,
  playImageSequence = null,
  animationDelay = 5000, // Default delay of 5 seconds
  onTickerComplete = null, // Callback after a full cycle is complete
  loop = false             // NEW: Optional loop flag (default false)
}) {
  const textWrapper = document.querySelector(textSelector);
  const canvas = document.querySelector(canvasSelector);
  let topData = [];
  let currentIndex = 0;
  let previousTopData = [];

  async function fetchTickerData() {
    try {
      const response = await fetch(apiUrl);
      const data = await response.json();
      const newTopData = data.map(item => item[dataSourceKey]);

      if (JSON.stringify(newTopData) !== JSON.stringify(previousTopData)) {
        previousTopData = newTopData;
        topData = newTopData;
        resetTicker();
        startTickerAnimation();
      }
    } catch (error) {
      textWrapper.textContent = "Error loading data";
      console.error(error);
    }
  }

  function resetTicker() {
    currentIndex = 0;
    textWrapper.textContent = "";
  }

  function startTickerAnimation() {
    if (!topData.length) return;

    function animateText() {
      // Set the current text and prepare spans
      textWrapper.textContent = topData[currentIndex];
      splitTextToSpans(textSelector);

      playAnimation(textSelector, textAnimationName, animationDelay, () => {
        // If we are at the last item in the ticker…
        if (currentIndex === topData.length - 1) {
          if (typeof playImageSequence === "function") {
            playImageSequence(() => {
              // Call the ticker complete callback if provided.
              if (onTickerComplete && typeof onTickerComplete === "function") {
                onTickerComplete();
              }
              // If looping is enabled, restart the ticker
              if (loop) {
                currentIndex = 0;
                animateText();
              }
            });
          } else {
            if (onTickerComplete && typeof onTickerComplete === "function") {
              onTickerComplete();
            }
            if (loop) {
              currentIndex = 0;
              animateText();
            }
          }
        } else {
          // Otherwise, move on to the next ticker text.
          if (typeof playImageSequence === "function") {
            playImageSequence(() => {
              currentIndex++;
              animateText();
            });
          } else {
            currentIndex++;
            animateText();
          }
        }
      });
    }

    animateText();
  }

  // Fetch ticker data every 5 seconds.
  setInterval(fetchTickerData, 5000);
  fetchTickerData();
}


// Device pixel ratio adjustment
if (window.devicePixelRatio !== 1) {
    const scaleFactor = window.devicePixelRatio;
    document.body.style.transform = `scale(${1 / scaleFactor})`;
    document.body.style.transformOrigin = "0 0";
}
