// Copyright Bill Tracey (KD5TFD) 2006 
// email: bill@ewjt.com 
//
// routines to manage a fifo 

#include <stddef.h>
#include "private.h" 

#define NUM_BLOCKS_IN_POOL (5) 
#define POOL_BUF_SIZE (32000) 

typedef struct pool_element { 	
	int state;  // 0 = free, 1 = busy 
    unsigned char data[POOL_BUF_SIZE]; 
}  POOL_ELEMENT; 

typedef struct fifo_element { 
	struct fifo_element *nextp; 
	struct fifo_element *prevp; 	
	int dlen;              // length of data field 
	char data[1];          // data - must be last element in struct 
} FIFO_ELEMENT; 

typedef struct fifo { 
	sem_t wait_sem;         // semaphore for people waiting for item to be readable  
	pthread_mutex_t mutex;  // mutex protecting the data struct 	
	FIFO_ELEMENT *frontp;   // front of fifo - this is next item read 
	FIFO_ELEMENT *backp;    // back of fifo - items inserted are inserted behind this one 
	int free_pool_idx; 
	POOL_ELEMENT block_pool[NUM_BLOCKS_IN_POOL]; 
} FIFO; 


void *createFIFO(void) { 
	FIFO *fifop;
	int i; 
	
	fifop = (FIFO *)malloc(sizeof(FIFO)); 
	if ( fifop == NULL ) return NULL; 

	sem_init(&(fifop->wait_sem),0,0); 
	pthread_mutex_init(&(fifop->mutex), NULL); 
	fifop->frontp = NULL; 
	fifop->backp = NULL; 
	for ( i = 0; i < NUM_BLOCKS_IN_POOL; i++ ) { 
		fifop->block_pool[i].state = 0; 
	}
	fifop->free_pool_idx = 0; 
	return fifop; 
}

void destroyFIFO(void *fifoh) { 	
	FIFO *fifop = (FIFO *)fifoh; 
//	FIFO_ELEMENT *fep;
//	FIFO_ELEMENT *next_fep;
	pthread_mutex_lock(&(fifop->mutex));
#if 0 
	fep = fifop->backp; 
	while ( fep != NULL ) { 
		next_fep = fep->nextp; 
		free(fep); 
		fep = next_fep; 
	} 
#endif 
	fifop->backp = NULL; 
	fifop->frontp = NULL; 
	// suspect this term code is not quite correct! 
	sem_post_multiple(&(fifop->wait_sem), 0x7fff); // unblock anyone waiting 
	sched_yield(); // let other threads run 
	sem_destroy(&(fifop->wait_sem)); 
	pthread_mutex_unlock(&(fifop->mutex)); 
	pthread_mutex_destroy(&(fifop->mutex)); 
	free(fifop); 
	return;
} 

FIFO_ELEMENT *getFEP(FIFO *fifop) { 
	POOL_ELEMENT *ep; 
	ep = &(fifop->block_pool[fifop->free_pool_idx]);
	if ( ep->state != 0 ) {  // block in use -- bail out 
		return NULL; 
	} 	
	/* else */ 
	++fifop->free_pool_idx; 
	if ( fifop->free_pool_idx >= NUM_BLOCKS_IN_POOL ) { 
		fifop->free_pool_idx = 0; 
	}
	
	ep->state = 1; 
	return (FIFO_ELEMENT *)(ep->data); 
}

void freeFEP(FIFO_ELEMENT *fep) { 
	POOL_ELEMENT *ep; 
	ep = (POOL_ELEMENT *)((unsigned char *)fep - offsetof(POOL_ELEMENT, data)); 
	ep->state = 0; // mark it free 
	return; 
} 

// 
// put a block of data into the fifo 
// returns 0 on success !0 otherwise 
// 2 -- no memory 
// 3 -- mutex failed 
int putFIFO(void *fifoh, void *datap, int dlen) { 

	FIFO *fifop; 
	FIFO_ELEMENT *new_elementp; 
	int rc; 

	fifop = (FIFO *)fifoh; 

	// get the mutex 
	rc = pthread_mutex_lock(&(fifop->mutex)); 
	if ( rc != 0 ) {  // failed! 	
		return 3; 
	} 

	// new_elementp = (FIFO_ELEMENT *)malloc(dlen + sizeof(FIFO_ELEMENT));
	new_elementp = getFEP(fifop); 
	if ( new_elementp == NULL ) { 
		pthread_mutex_unlock(&(fifop->mutex)); 
		return 2; 
	}

	new_elementp->dlen = dlen; 	
	memcpy(new_elementp->data, datap, dlen); 
	new_elementp->prevp = NULL; 

	
	// ok now have mutex -- link us in 
	new_elementp->nextp = fifop->backp; 
	if ( fifop->backp != NULL ) fifop->backp->prevp = new_elementp; 
	fifop->backp = new_elementp; 
	if ( fifop->frontp == NULL ) fifop->frontp = new_elementp; 
	sem_post(&(fifop->wait_sem));
	pthread_mutex_unlock(&(fifop->mutex)); 
	return 0; 	
}


void freeFIFOdata(void *p) { 
	void *addr_to_free; 
	addr_to_free = (char *)p - offsetof(FIFO_ELEMENT, data[0]);
	// free(addr_to_free); 
	freeFEP((FIFO_ELEMENT *)addr_to_free); 
} 

// get data out of a fifo - blocks until data is available 
// NULL returned on error. 
// pointer returned must be freed via freeFIFOdata(
// set wait to non zero if caller wants to wait for data to become available 
void *getFIFO(void *fifoh, int *dlenp, int wait) { 
	FIFO *fifop; 
	FIFO_ELEMENT *fep = NULL; 
	int rc; 
	fifop = (FIFO *)fifoh; 
	while ( fep == NULL ) { 
		if ( wait ) { 
			rc = sem_wait(&(fifop->wait_sem)); 
			if ( rc != 0 ) { // sem failed; 
				return NULL; 
			} 
		}
		else {  // no wait case 
			rc = sem_trywait(&(fifop->wait_sem)); 
			if ( rc == -1 ) {  // no data available 
				return NULL; 
			} 
		}
		rc = pthread_mutex_lock(&(fifop->mutex)); 
		if ( rc != 0 ) {  // mutex failed 
			return NULL; 
		} 		
		// mutex locked at this point -- grab our data 
		if ( fifop->frontp != NULL ) { 
			fep = fifop->frontp; 
			fifop->frontp = fep->prevp; 
		} 
		pthread_mutex_unlock(&(fifop->mutex)); 
		if ( !wait && fep == NULL ) {  // we don't go back thru loop if !wait and no data 
			return NULL; 
		}
	} 
	if ( dlenp != NULL ) *dlenp = fep->dlen; 
	return &(fep->data[0]); 
} 
