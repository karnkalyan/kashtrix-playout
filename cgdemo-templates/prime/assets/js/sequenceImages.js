function playImageSequenceAnimation(
  canvas, 
  imageFolder, 
  frameCount, 
  fps, 
  fileNameTemplate, 
  extension = '.png', 
  leadingZeros = 5, 
  startIndex = 0, 
  loop = true, 
  holdLastFrame = false, 
  onComplete = null, 
  triggerFrame = null,  // Frame at which to trigger callback
  onTrigger = null,     // Function to execute at trigger frame
  frameRange = null     // Array [startFrame, endFrame] to loop between *after* main sequence completes
) {
  const ctx = canvas.getContext('2d');
  const images = [];
  let currentFrame = 0;
  let isPlaying = true;
  let imageLoadedCount = 0;

  // Load images
  for (let i = 0; i < frameCount; i++) {
    const img = new Image();
    const paddedIndex = (startIndex + i).toString().padStart(leadingZeros, '0');
    const fileName = fileNameTemplate.replace("{index}", paddedIndex);
    img.src = `${imageFolder}${fileName}${extension}`;

    img.onload = () => {
      imageLoadedCount++;
      if (imageLoadedCount === 1) {
        canvas.width = img.width;
        canvas.height = img.height;
      }
      if (imageLoadedCount === frameCount) {
        playImageFrames(); // Start playing when all images are loaded
      }
    };

    images.push(img);
  }

  // Main animation loop (plays the full sequence)
  function playImageFrames() {
    if (!isPlaying) return;
    
    // Draw current frame
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.drawImage(images[currentFrame], 0, 0, canvas.width, canvas.height);
    
    // Trigger callback at specific frame if set
    if (onTrigger && typeof onTrigger === 'function' && currentFrame === triggerFrame) {
      onTrigger();
    }
    
    // Check if we've reached the end of the main sequence (only applies when loop is false)
    if (!loop && currentFrame === frameCount - 1) {
      // Call onComplete callback
      if (onComplete && typeof onComplete === 'function') {
        onComplete();
      }
      // If a frame range is provided, start the extra subrange loop instead of ending
      if (frameRange && Array.isArray(frameRange) && frameRange.length === 2) {
        playSubRangeAnimation(frameRange[0], frameRange[1]);
      } else {
        if (!holdLastFrame) {
          ctx.clearRect(0, 0, canvas.width, canvas.height);
        }
      }
      return;
    }
    
    // Update currentFrame
    if (loop) {
      currentFrame = (currentFrame + 1) % frameCount;
    } else {
      currentFrame++;
    }
    
    setTimeout(playImageFrames, 1000 / fps);
  }
  
  // Extra function: continuously loop between specified subrange frames
  function playSubRangeAnimation(startFrame, endFrame) {
    let subFrame = startFrame;
    
    function subLoop() {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.drawImage(images[subFrame], 0, 0, canvas.width, canvas.height);
      
      // Update subFrame index within the range
      if (subFrame < endFrame) {
        subFrame++;
      } else {
        subFrame = startFrame;
      }
      
      setTimeout(subLoop, 1000 / fps);
    }
    
    subLoop();
  }
  
  // Toggle play/pause on click (applies only to the main sequence)
  canvas.addEventListener('click', () => {
    isPlaying = !isPlaying;
    if (isPlaying) playImageFrames();
  });
}